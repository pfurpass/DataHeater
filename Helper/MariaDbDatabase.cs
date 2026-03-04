using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Linq;

namespace DataHeater.Helper
{
    internal class MariaDbDatabase
    {
        private readonly string _connectionString;

        public MariaDbDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<string>> GetTablesAsync()
        {
            var tables = new List<string>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SHOW TABLES;", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));

            return tables;
        }

        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);
            using var adapter = new MySqlDataAdapter(cmd);

            var table = new DataTable();
            adapter.Fill(table);

            return table;
        }

        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var columns = new List<string>();

            foreach (DataColumn col in schema.Columns)
            {
                string type = MapType(col.DataType);
                columns.Add($"`{col.ColumnName}` {type}");
            }

            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(",", columns)})";

            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (DataRow row in data.Rows)
            {
                var columns = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"`{c.ColumnName}`"));
                var values = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"@{c.ColumnName}"));

                string sql = $"INSERT INTO `{tableName}` ({columns}) VALUES ({values})";

                using var cmd = new MySqlCommand(sql, conn);

                foreach (DataColumn col in data.Columns)
                    cmd.Parameters.AddWithValue($"@{col.ColumnName}", row[col]);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private string MapType(Type type)
        {
            if (type == typeof(int)) return "INT";
            if (type == typeof(long)) return "BIGINT";
            if (type == typeof(double)) return "DOUBLE";
            if (type == typeof(decimal)) return "DECIMAL(18,2)";
            if (type == typeof(DateTime)) return "DATETIME";
            if (type == typeof(bool)) return "BOOLEAN";
            return "TEXT";
        }
    }
}