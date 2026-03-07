using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DataHeater.Helper
{
    internal class SqliteDatabase : ITargetDatabase
    {
        private readonly string _cs;
        public SqliteDatabase(string connectionString) => _cs = connectionString;

        // SQLite verträgt keine Sonderzeichen in Spaltennamen
        private static string San(string s)
            => s.Replace("#", "_").Replace("$", "_").Replace(" ", "_").Replace("-", "_");

        // ── GetTablesAsync ─────────────────────────────────────────────────
        public async Task<List<string>> GetTablesAsync()
        {
            var list = new List<string>();
            await using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new SqliteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0));
            return list;
        }

        // ── GetTableDataAsync ──────────────────────────────────────────────
        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            await using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync();

            var colTypes = new System.Collections.Generic.Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            await using (var p = new SqliteCommand($"PRAGMA table_info(`{tableName}`)", conn))
            await using (var pr = await p.ExecuteReaderAsync())
                while (await pr.ReadAsync())
                    colTypes[pr.GetString(1)] = pr.GetString(2).ToUpperInvariant().Trim();

            await using var cmd = new SqliteCommand($"SELECT * FROM `{tableName}`", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < r.FieldCount; i++)
            {
                string name = r.GetName(i);
                string type = colTypes.TryGetValue(name, out var t) ? t : "TEXT";
                var col = new DataColumn(name, typeof(string));
                col.ExtendedProperties["ColumnInfo"] = TypeMapper.FromSqlite(name, type);
                table.Columns.Add(col);
            }

            while (await r.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < r.FieldCount; i++)
                    // Nur echtes DBNull → NULL; alle anderen Werte 1:1 übernehmen
                    row[i] = r.IsDBNull(i) ? DBNull.Value : (object)r.GetValue(i).ToString();
                table.Rows.Add(row);
            }
            return table;
        }

        // ── CreateTableIfNotExistsAsync ────────────────────────────────────
        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            await using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync();
            var cols = schema.Columns.Cast<DataColumn>()
                .Select(c => $"`{San(c.ColumnName)}` {TypeMapper.ToSqlite(TypeMapper.FromDataColumn(c))}");
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(", ", cols)})";
            System.Diagnostics.Debug.WriteLine("[SQLite] " + sql);
            await using var cmd = new SqliteCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── TruncateTableAsync ─────────────────────────────────────────────
        public async Task TruncateTableAsync(string tableName)
        {
            await using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new SqliteCommand($"DELETE FROM `{tableName}`", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── InsertDataAsync ────────────────────────────────────────────────
        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            await using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync();

            var colNames = string.Join(", ", data.Columns.Cast<DataColumn>()
                                 .Select(c => $"`{San(c.ColumnName)}`"));
            var paramNames = string.Join(", ", data.Columns.Cast<DataColumn>()
                                 .Select(c => $"@p_{San(c.ColumnName)}"));
            string sql = $"INSERT INTO `{tableName}` ({colNames}) VALUES ({paramNames})";

            foreach (DataRow row in data.Rows)
            {
                await using var cmd = new SqliteCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                {
                    string pname = $"@p_{San(col.ColumnName)}";
                    string raw = DbConverter.ToSafeString(row[col]); // null = war wirklich NULL
                    if (raw == null)
                    { cmd.Parameters.AddWithValue(pname, DBNull.Value); continue; }

                    var info = TypeMapper.FromDataColumn(col);
                    string conv = DbConverter.ConvertToString(raw, info);
                    // conv ist NIE null wenn raw != null (Fallback = raw selbst)
                    cmd.Parameters.AddWithValue(pname, conv);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}