using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace DataHeater.Helper
{
    internal class SqliteDatabase : ITargetDatabase
    {
        private readonly string _connectionString;

        public SqliteDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<string>> GetTablesAsync()
        {
            var tables = new List<string>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
            return tables;
        }

        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var pragmaCmd = new SqliteCommand($"PRAGMA table_info(`{tableName}`)", conn))
            using (var pragmaReader = await pragmaCmd.ExecuteReaderAsync())
            {
                while (await pragmaReader.ReadAsync())
                    columnTypes[pragmaReader.GetString(1)] = pragmaReader.GetString(2).ToUpper().Trim();
            }

            using var cmd = new SqliteCommand($"SELECT * FROM `{tableName}`", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                string sqliteType = columnTypes.ContainsKey(name) ? columnTypes[name] : "TEXT";
                var col = new DataColumn(name, typeof(string));
                col.ExtendedProperties["SqliteType"] = sqliteType;
                table.Columns.Add(col);
            }

            while (await reader.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.IsDBNull(i)) { row[i] = DBNull.Value; continue; }
                    string val = reader.GetValue(i)?.ToString();
                    row[i] = string.IsNullOrWhiteSpace(val) ||
                              val.Equals("null", StringComparison.OrdinalIgnoreCase)
                        ? DBNull.Value : (object)val;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                UniversalType utype = TypeMapper.FromExtendedProperties(col);
                string sqliteType = TypeMapper.ToSqlite(utype);
                columns.Add($"`{col.ColumnName}` {sqliteType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(", ", columns)})";
            System.Diagnostics.Debug.WriteLine(sql);
            using var cmd = new SqliteCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task TruncateTableAsync(string tableName)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand($"DELETE FROM `{tableName}`", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            foreach (DataRow row in data.Rows)
            {
                var cols = string.Join(", ", data.Columns.Cast<DataColumn>()
                    .Select(c => $"`{c.ColumnName}`"));
                var vals = string.Join(", ", data.Columns.Cast<DataColumn>()
                    .Select(c => $"@p_{c.ColumnName}"));
                string sql = $"INSERT INTO `{tableName}` ({cols}) VALUES ({vals})";
                using var cmd = new SqliteCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                {
                    string safe = DbConverter.ToSafeString(row[col]);
                    if (safe == null)
                    {
                        cmd.Parameters.AddWithValue($"@p_{col.ColumnName}", DBNull.Value);
                        continue;
                    }
                    UniversalType utype = TypeMapper.FromExtendedProperties(col);
                    string converted = DbConverter.ConvertToString(safe, utype);
                    cmd.Parameters.AddWithValue($"@p_{col.ColumnName}",
                        converted != null ? (object)converted : DBNull.Value);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}