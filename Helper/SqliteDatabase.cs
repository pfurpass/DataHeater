using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace DataHeater.Helper
{
    internal class SqliteDatabase
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
            string sql = "SELECT name FROM sqlite_master WHERE type='table';";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
            return tables;
        }

        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand($"SELECT * FROM `{tableName}`", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();
            for (int i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i));
            while (await reader.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
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
                columns.Add($"`{col.ColumnName}` TEXT");
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(",", columns)})";
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
                var columns = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"`{c.ColumnName}`"));
                var values = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"@{c.ColumnName}"));
                string sql = $"INSERT INTO `{tableName}` ({columns}) VALUES ({values})";
                using var cmd = new SqliteCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                    cmd.Parameters.AddWithValue($"@{col.ColumnName}", row[col] ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}