using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace DataHeater.Helper
{
    internal class OracleDatabase : ITargetDatabase
    {
        private readonly string _cs, _csNodb, _db;
        private readonly bool _create;

        public OracleDatabase(string cs, string csNodb, string db, bool create)
        { _cs = cs; _csNodb = csNodb; _db = db; _create = create; }

        // Oracle: Datenbank = Schema = User
        private async Task EnsureDbAsync()
        {
            if (!_create) return;
            try
            {
                await using var conn = new OracleConnection(_csNodb);
                await conn.OpenAsync();
                await using var check = new OracleCommand(
                    "SELECT COUNT(*) FROM dba_users WHERE username = :n", conn);
                check.Parameters.Add("n", _db.ToUpper());
                decimal exists = (decimal)await check.ExecuteScalarAsync();
                if (exists == 0)
                {
                    await using var create = new OracleCommand(
                        $"CREATE USER \"{_db.ToUpper()}\" IDENTIFIED BY \"{_db}\" " +
                        "DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS", conn);
                    await create.ExecuteNonQueryAsync();
                    await using var grant = new OracleCommand(
                        $"GRANT CONNECT, RESOURCE TO \"{_db.ToUpper()}\"", conn);
                    await grant.ExecuteNonQueryAsync();
                }
            }
            catch { /* kein DBA-Zugriff — ignorieren */ }
        }

        // ── GetTablesAsync ─────────────────────────────────────────────────
        public async Task<List<string>> GetTablesAsync()
        {
            await EnsureDbAsync();
            var list = new List<string>();
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(
                "SELECT table_name FROM user_tables " +
                "WHERE table_name NOT LIKE '%$%' " +
                "AND table_name NOT LIKE 'MVIEW%' " +
                "AND table_name NOT LIKE 'OL$%' " +
                "AND table_name NOT LIKE 'AQ$%' " +
                "AND table_name NOT LIKE 'REDO%' " +
                "AND table_name NOT LIKE 'SYS_%' " +
                "AND table_name NOT IN ('SQLPLUS_PRODUCT_PROFILE','HELP'," +
                "'PRODUCT_USER_PROFILE','REDO_DB','REDO_LOG') " +
                "ORDER BY table_name", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0).ToLower());
            return list;
        }

        // ── GetTableDataAsync ──────────────────────────────────────────────
        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            // Spaltenmetadaten lesen
            var colInfos = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);
            await using (var sc = new OracleCommand(
                "SELECT column_name, data_type, data_precision, data_scale, " +
                "data_length, char_length " +
                "FROM user_tab_columns WHERE table_name = :t ORDER BY column_id", conn))
            {
                sc.Parameters.Add("t", tableName.ToUpper());
                await using var sr = await sc.ExecuteReaderAsync();
                while (await sr.ReadAsync())
                {
                    string colName = sr.GetString(0).ToLower();
                    string dataType = sr.GetString(1).ToUpperInvariant();

                    if (dataType == "NUMBER")
                    {
                        if (!sr.IsDBNull(2))
                        {
                            int prec = sr.GetInt32(2);
                            int scale = sr.IsDBNull(3) ? 0 : sr.GetInt32(3);
                            dataType = $"NUMBER({prec},{scale})";
                        }
                    }
                    else if (dataType is "VARCHAR2" or "CHAR")
                    {
                        int charLen = sr.IsDBNull(5) ? 0 : sr.GetInt32(5);
                        dataType = $"{dataType}({charLen})";
                    }

                    colInfos[colName] = TypeMapper.FromOracle(colName, dataType);
                }
            }

            await using var cmd = new OracleCommand(
                $"SELECT * FROM \"{tableName.ToUpper()}\"", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < r.FieldCount; i++)
            {
                string name = r.GetName(i).ToLower();
                var col = new DataColumn(name, typeof(string));
                col.ExtendedProperties["ColumnInfo"] = colInfos.TryGetValue(name, out var ci)
                    ? ci : new ColumnInfo { Name = name, DotNetType = typeof(string) };
                table.Columns.Add(col);
            }

            while (await r.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < r.FieldCount; i++)
                {
                    if (r.IsDBNull(i)) { row[i] = DBNull.Value; continue; }

                    string colName = r.GetName(i).ToLower();
                    var info = colInfos.TryGetValue(colName, out var ci)
                        ? ci : new ColumnInfo { DotNetType = typeof(string) };

                    // DATE/TIMESTAMP → via GetDateTime lesen (NLS-unabhängig)
                    if (info.DateKind is DbDateKind.DateOnly or DbDateKind.DateTime)
                    {
                        try
                        {
                            DateTime dt = r.GetDateTime(i);
                            row[i] = info.DateKind == DbDateKind.DateOnly
                                ? dt.ToString("yyyy-MM-dd")
                                : dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch
                        {
                            // Fallback: Rohwert, niemals NULL
                            string raw = r.GetValue(i)?.ToString();
                            row[i] = string.IsNullOrEmpty(raw) ? DBNull.Value : (object)raw;
                        }
                    }
                    else
                    {
                        // Alle anderen Typen: Rohstring, niemals leer durch NULL ersetzen
                        row[i] = r.GetValue(i).ToString();
                    }
                }
                table.Rows.Add(row);
            }
            return table;
        }

        // ── CreateTableIfNotExistsAsync ────────────────────────────────────
        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            await EnsureDbAsync();
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            await using var check = new OracleCommand(
                "SELECT COUNT(*) FROM user_tables WHERE table_name = :t", conn);
            check.Parameters.Add("t", tableName.ToUpper());
            if ((decimal)await check.ExecuteScalarAsync() > 0) return;

            var cols = schema.Columns.Cast<DataColumn>()
                .Select(c => $"\"{c.ColumnName.ToUpper()}\" {TypeMapper.ToOracle(TypeMapper.FromDataColumn(c))}");
            string sql = $"CREATE TABLE \"{tableName.ToUpper()}\" ({string.Join(", ", cols)})";
            System.Diagnostics.Debug.WriteLine("[Oracle] " + sql);
            await using var cmd = new OracleCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── TruncateTableAsync ─────────────────────────────────────────────
        // DELETE statt TRUNCATE → benötigt weniger Oracle-Rechte
        public async Task TruncateTableAsync(string tableName)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(
                $"DELETE FROM \"{tableName.ToUpper()}\"", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── InsertDataAsync ────────────────────────────────────────────────
        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            await using var conn = new OracleConnection(_cs);
            await conn.OpenAsync();

            foreach (DataRow row in data.Rows)
            {
                var colParts = new List<string>();
                var valParts = new List<string>();

                foreach (DataColumn col in data.Columns)
                {
                    colParts.Add($"\"{col.ColumnName.ToUpper()}\"");
                    string raw = DbConverter.ToSafeString(row[col]);
                    var info = TypeMapper.FromDataColumn(col);

                    if (raw != null)
                    {
                        string conv = DbConverter.ConvertToString(raw, info);
                        switch (info.DateKind)
                        {
                            case DbDateKind.DateOnly:
                                valParts.Add($"TO_DATE(:p_{col.ColumnName}, 'YYYY-MM-DD')");
                                break;
                            case DbDateKind.DateTime:
                                valParts.Add($"TO_TIMESTAMP(:p_{col.ColumnName}, 'YYYY-MM-DD HH24:MI:SS')");
                                break;
                            default:
                                valParts.Add($":p_{col.ColumnName}");
                                break;
                        }
                    }
                    else
                    {
                        valParts.Add($":p_{col.ColumnName}");
                    }
                }

                string sql = $"INSERT INTO \"{tableName.ToUpper()}\" " +
                             $"({string.Join(", ", colParts)}) " +
                             $"VALUES ({string.Join(", ", valParts)})";
                await using var cmd = new OracleCommand(sql, conn);

                foreach (DataColumn col in data.Columns)
                {
                    string pname = $"p_{col.ColumnName}";
                    string raw = DbConverter.ToSafeString(row[col]);

                    if (raw == null)
                    { cmd.Parameters.Add(pname, DBNull.Value); continue; }

                    var info = TypeMapper.FromDataColumn(col);
                    string conv = DbConverter.ConvertToString(raw, info);
                    // conv ist niemals null (Fallback = raw)

                    // Datum → String, TO_DATE/TO_TIMESTAMP macht Oracle daraus
                    if (info.DateKind is DbDateKind.DateOnly or DbDateKind.DateTime)
                    { cmd.Parameters.Add(pname, conv); continue; }

                    // Numerische Typen → typisiert übergeben (NLS-sicher)
                    if (info.DotNetType == typeof(long) || info.DotNetType == typeof(int))
                    {
                        if (long.TryParse(conv, out long l))
                        { cmd.Parameters.Add(new OracleParameter(pname, OracleDbType.Int64) { Value = l }); }
                        else
                        { cmd.Parameters.Add(pname, conv); } // Fallback: als String
                        continue;
                    }
                    if (info.DotNetType == typeof(double))
                    {
                        if (double.TryParse(conv, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out double d))
                        { cmd.Parameters.Add(new OracleParameter(pname, OracleDbType.BinaryDouble) { Value = d }); }
                        else
                        { cmd.Parameters.Add(pname, conv); }
                        continue;
                    }
                    if (info.DotNetType == typeof(decimal))
                    {
                        if (decimal.TryParse(conv, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out decimal d))
                        { cmd.Parameters.Add(new OracleParameter(pname, OracleDbType.Decimal) { Value = d }); }
                        else
                        { cmd.Parameters.Add(pname, conv); }
                        continue;
                    }
                    if (info.DotNetType == typeof(bool))
                    {
                        int bval = (conv == "1") ? 1 : 0;
                        cmd.Parameters.Add(new OracleParameter(pname, OracleDbType.Int32) { Value = bval });
                        continue;
                    }

                    cmd.Parameters.Add(pname, conv);
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}