using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Linq;

namespace DataHeater.Helper
{
    internal class MariaDbDatabase : ITargetDatabase
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

            var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var schemaCmd = new MySqlCommand(
                "SELECT COLUMN_NAME, COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t ORDER BY ORDINAL_POSITION", conn))
            {
                schemaCmd.Parameters.AddWithValue("@t", tableName);
                using var schemaReader = await schemaCmd.ExecuteReaderAsync();
                while (await schemaReader.ReadAsync())
                    columnTypes[schemaReader.GetString(0)] = schemaReader.GetString(1).ToUpper();
            }

            using var cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);
            using var adapter = new MySqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);

            foreach (DataColumn col in table.Columns)
                if (columnTypes.ContainsKey(col.ColumnName))
                    col.ExtendedProperties["MariaDbType"] = columnTypes[col.ColumnName];

            return table;
        }

        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                string sourceType =
                    col.ExtendedProperties.Contains("SqliteType") ? col.ExtendedProperties["SqliteType"].ToString().ToUpper() :
                    col.ExtendedProperties.Contains("PostgresType") ? col.ExtendedProperties["PostgresType"].ToString().ToUpper() :
                    null;
                string mariaType = MapToMariaDb(col.DataType, sourceType);
                columns.Add($"`{col.ColumnName}` {mariaType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(",", columns)})";
            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task TruncateTableAsync(string tableName)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand($"TRUNCATE TABLE `{tableName}`", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            foreach (DataRow row in data.Rows)
            {
                var cols = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"`{c.ColumnName}`"));
                var vals = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"@{c.ColumnName}"));
                string sql = $"INSERT INTO `{tableName}` ({cols}) VALUES ({vals})";
                using var cmd = new MySqlCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                    cmd.Parameters.AddWithValue($"@{col.ColumnName}",
                        row[col] == DBNull.Value ? DBNull.Value : row[col]);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private string MapToMariaDb(Type type, string sourceType = null)
        {
            if (sourceType != null)
            {
                if (sourceType == "DATE") return "DATE";
                if (sourceType.Contains("DATETIME") || sourceType.Contains("TIMESTAMP")) return "DATETIME";
                if (sourceType.Contains("TIME")) return "TIME";
                if (sourceType.Contains("TINYINT(1)") || sourceType.Contains("BOOL")) return "BOOLEAN";
                if (sourceType.Contains("INT4") || sourceType.Contains("INT8")) return "BIGINT";
                if (sourceType.Contains("INT")) return "BIGINT";
                if (sourceType.Contains("FLOAT8") || sourceType.Contains("FLOAT4")
                                                   || sourceType.Contains("DOUBLE")
                                                   || sourceType.Contains("FLOAT")) return "DOUBLE";
                if (sourceType.Contains("NUMERIC") || sourceType.Contains("DECIMAL")) return "DECIMAL(18,2)";
                if (sourceType.Contains("BYTEA") || sourceType.Contains("BLOB")) return "LONGBLOB";
                if (sourceType.Contains("TEXT")) return "TEXT";
                if (sourceType.StartsWith("VARCHAR") || sourceType.StartsWith("CHAR")) return sourceType;
                if (!string.IsNullOrWhiteSpace(sourceType)) return sourceType;
            }

            if (type == typeof(long) || type == typeof(int)) return "BIGINT";
            if (type == typeof(double) || type == typeof(float)) return "DOUBLE";
            if (type == typeof(decimal)) return "DECIMAL(18,2)";
            if (type == typeof(bool)) return "BOOLEAN";
            if (type == typeof(DateTime)) return "DATETIME";
            if (type == typeof(byte[])) return "LONGBLOB";
            return "TEXT";
        }
    }
}