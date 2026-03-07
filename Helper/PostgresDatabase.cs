using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace DataHeater.Helper
{
    internal class PostgresDatabase : ITargetDatabase
    {
        private readonly string _cs, _csNodb, _db;
        private readonly bool _create;

        public PostgresDatabase(string cs, string csNodb, string db, bool create)
        { _cs = cs; _csNodb = csNodb; _db = db; _create = create; }

        private async Task EnsureDbAsync()
        {
            if (!_create) return;
            await using var conn = new NpgsqlConnection(_csNodb);
            await conn.OpenAsync();
            await using var check = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname=@n", conn);
            check.Parameters.AddWithValue("@n", _db.ToLower());
            if (await check.ExecuteScalarAsync() == null)
            {
                await using var create = new NpgsqlCommand(
                    $"CREATE DATABASE \"{_db}\" ENCODING 'UTF8';", conn);
                await create.ExecuteNonQueryAsync();
            }
        }

        // ── GetTablesAsync ─────────────────────────────────────────────────
        public async Task<List<string>> GetTablesAsync()
        {
            await EnsureDbAsync();
            var list = new List<string>();
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT tablename FROM pg_tables WHERE schemaname='public';", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0));
            return list;
        }

        // ── GetTableDataAsync ──────────────────────────────────────────────
        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            var colTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using (var sc = new NpgsqlCommand(
                "SELECT column_name, udt_name FROM information_schema.columns " +
                "WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position", conn))
            {
                sc.Parameters.AddWithValue("@t", tableName);
                await using var sr = await sc.ExecuteReaderAsync();
                while (await sr.ReadAsync())
                    colTypes[sr.GetString(0)] = sr.GetString(1).ToUpperInvariant();
            }

            await using var cmd = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < r.FieldCount; i++)
            {
                string name = r.GetName(i);
                string type = colTypes.TryGetValue(name, out var t) ? t : "TEXT";
                var col = new DataColumn(name, typeof(string));
                col.ExtendedProperties["ColumnInfo"] = TypeMapper.FromPostgres(name, type);
                table.Columns.Add(col);
            }

            while (await r.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < r.FieldCount; i++)
                    row[i] = r.IsDBNull(i) ? DBNull.Value : (object)r.GetValue(i).ToString();
                table.Rows.Add(row);
            }
            return table;
        }

        // ── CreateTableIfNotExistsAsync ────────────────────────────────────
        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            await EnsureDbAsync();
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            var cols = schema.Columns.Cast<DataColumn>()
                .Select(c => $"\"{c.ColumnName}\" {TypeMapper.ToPostgres(TypeMapper.FromDataColumn(c))}");
            string sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", cols)})";
            System.Diagnostics.Debug.WriteLine("[PG] " + sql);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── TruncateTableAsync ─────────────────────────────────────────────
        public async Task TruncateTableAsync(string tableName)
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"TRUNCATE TABLE \"{tableName}\"", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── InsertDataAsync ────────────────────────────────────────────────
        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            var colNames = string.Join(", ", data.Columns.Cast<DataColumn>()
                                 .Select(c => $"\"{c.ColumnName}\""));
            var paramNames = string.Join(", ", data.Columns.Cast<DataColumn>()
                                 .Select(c => $"@p_{c.ColumnName}"));
            string sql = $"INSERT INTO \"{tableName}\" ({colNames}) VALUES ({paramNames})";

            foreach (DataRow row in data.Rows)
            {
                await using var cmd = new NpgsqlCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                {
                    string pname = $"@p_{col.ColumnName}";
                    string raw = DbConverter.ToSafeString(row[col]);
                    if (raw == null)
                    { cmd.Parameters.AddWithValue(pname, DBNull.Value); continue; }

                    var info = TypeMapper.FromDataColumn(col);
                    object obj = DbConverter.ConvertForPostgres(raw, info);

                    // Typisierte Parameter für Datumsspalten
                    switch (info.DateKind)
                    {
                        case DbDateKind.DateOnly:
                            var pd = new NpgsqlParameter(pname, NpgsqlDbType.Date)
                            { Value = obj is string ? DBNull.Value : obj };
                            cmd.Parameters.Add(pd); break;
                        case DbDateKind.DateTime:
                            var pdt = new NpgsqlParameter(pname, NpgsqlDbType.Timestamp)
                            { Value = obj is string ? DBNull.Value : obj };
                            cmd.Parameters.Add(pdt); break;
                        case DbDateKind.TimeOnly:
                            var pt = new NpgsqlParameter(pname, NpgsqlDbType.Time)
                            { Value = obj is string ? DBNull.Value : obj };
                            cmd.Parameters.Add(pt); break;
                        default:
                            cmd.Parameters.AddWithValue(pname, obj ?? DBNull.Value); break;
                    }
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}