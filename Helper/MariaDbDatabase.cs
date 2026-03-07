using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DataHeater.Helper
{
    internal class MariaDbDatabase : ITargetDatabase
    {
        private readonly string _cs, _csNodb, _db;
        private readonly bool _create;

        public MariaDbDatabase(string cs, string csNodb, string db, bool create)
        { _cs = cs; _csNodb = csNodb; _db = db; _create = create; }

        private async Task EnsureDbAsync()
        {
            if (!_create) return;
            await using var conn = new MySqlConnection(_csNodb);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                $"CREATE DATABASE IF NOT EXISTS `{_db}` " +
                "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── GetTablesAsync ─────────────────────────────────────────────────
        public async Task<List<string>> GetTablesAsync()
        {
            await EnsureDbAsync();
            var list = new List<string>();
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SHOW TABLES;", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0));
            return list;
        }

        // ── GetTableDataAsync ──────────────────────────────────────────────
        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();

            var colTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using (var sc = new MySqlCommand(
                "SELECT COLUMN_NAME, COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t ORDER BY ORDINAL_POSITION", conn))
            {
                sc.Parameters.AddWithValue("@t", tableName);
                await using var sr = await sc.ExecuteReaderAsync();
                while (await sr.ReadAsync())
                    colTypes[sr.GetString(0)] = sr.GetString(1).ToUpperInvariant();
            }

            await using var cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < r.FieldCount; i++)
            {
                string name = r.GetName(i);
                string type = colTypes.TryGetValue(name, out var t) ? t : "TEXT";
                var col = new DataColumn(name, typeof(string));
                col.ExtendedProperties["ColumnInfo"] = TypeMapper.FromMariaDb(name, type);
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
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            var cols = schema.Columns.Cast<DataColumn>()
                .Select(c => $"`{c.ColumnName.Trim()}` {TypeMapper.ToMariaDb(TypeMapper.FromDataColumn(c))}");
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName.Trim()}` ({string.Join(", ", cols)})";
            System.Diagnostics.Debug.WriteLine("[MariaDB] " + sql);
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── TruncateTableAsync ─────────────────────────────────────────────
        public async Task TruncateTableAsync(string tableName)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand($"TRUNCATE TABLE `{tableName}`", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── InsertDataAsync ────────────────────────────────────────────────
        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();

            var colNames = string.Join(", ", data.Columns.Cast<DataColumn>()
                                 .Select(c => $"`{c.ColumnName.Trim()}`"));
            var paramNames = string.Join(", ", data.Columns.Cast<DataColumn>()
                                 .Select(c => $"@p_{c.ColumnName.Trim()}"));
            string sql = $"INSERT INTO `{tableName.Trim()}` ({colNames}) VALUES ({paramNames})";

            foreach (DataRow row in data.Rows)
            {
                await using var cmd = new MySqlCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                {
                    string pname = $"@p_{col.ColumnName.Trim()}";
                    string raw = DbConverter.ToSafeString(row[col]);
                    if (raw == null)
                    { cmd.Parameters.AddWithValue(pname, DBNull.Value); continue; }

                    var info = TypeMapper.FromDataColumn(col);
                    string conv = DbConverter.ConvertToString(raw, info);
                    cmd.Parameters.AddWithValue(pname, conv);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}