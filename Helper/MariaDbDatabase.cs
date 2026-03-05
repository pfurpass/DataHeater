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

            // Echte MariaDB Spaltentypen via INFORMATION_SCHEMA lesen
            var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var schemaCmd = new MySqlCommand(
                "SELECT COLUMN_NAME, COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t ORDER BY ORDINAL_POSITION", conn))
            {
                schemaCmd.Parameters.AddWithValue("@t", tableName);
                using var schemaReader = await schemaCmd.ExecuteReaderAsync();
                while (await schemaReader.ReadAsync())
                {
                    string colName = schemaReader.GetString(0);
                    string colType = schemaReader.GetString(1).ToUpper(); // z.B. "BIGINT(20)", "VARCHAR(255)", "DATE"
                    columnTypes[colName] = colType;
                }
            }

            using var cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);
            using var adapter = new MySqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);

            // MariaDB-Typ in ExtendedProperties speichern
            foreach (DataColumn col in table.Columns)
            {
                if (columnTypes.ContainsKey(col.ColumnName))
                    col.ExtendedProperties["MariaDbType"] = columnTypes[col.ColumnName];
            }

            return table;
        }

        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                string sqliteType = col.ExtendedProperties.Contains("SqliteType")
                    ? col.ExtendedProperties["SqliteType"].ToString().ToUpper()
                    : null;
                string mariaType = MapToMariaDb(col.DataType, sqliteType);
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

        private string MapToMariaDb(Type type, string sqliteType = null)
        {
            if (sqliteType != null)
            {
                if (sqliteType == "DATE") return "DATE";
                if (sqliteType.Contains("DATETIME")) return "DATETIME";
                if (sqliteType.Contains("TIME")) return "TIME";
                if (sqliteType.Contains("INT")) return "BIGINT";
                if (sqliteType.Contains("REAL") || sqliteType.Contains("FLOAT")
                                                || sqliteType.Contains("DOUBLE")) return "DOUBLE";
                if (sqliteType.Contains("NUMERIC") || sqliteType.Contains("DECIMAL")) return "DECIMAL(18,2)";
                if (sqliteType.Contains("BOOL")) return "BOOLEAN";
                if (sqliteType.Contains("BLOB")) return "LONGBLOB";
                if (sqliteType == "TEXT") return "TEXT";
                if (!string.IsNullOrWhiteSpace(sqliteType)) return sqliteType;
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