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
        private readonly string _connectionStringWithoutDb;
        private readonly string _databaseName;
        private readonly bool _createIfNotExists;

        public MariaDbDatabase(string connectionString, string connectionStringWithoutDb,
            string databaseName, bool createIfNotExists)
        {
            _connectionString = connectionString;
            _connectionStringWithoutDb = connectionStringWithoutDb;
            _databaseName = databaseName;
            _createIfNotExists = createIfNotExists;
        }

        private async Task EnsureDatabaseExistsAsync()
        {
            if (!_createIfNotExists) return;
            using var conn = new MySqlConnection(_connectionStringWithoutDb);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                $"CREATE DATABASE IF NOT EXISTS `{_databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
                conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetTablesAsync()
        {
            await EnsureDatabaseExistsAsync();
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
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                var col = new DataColumn(name, typeof(string));
                if (columnTypes.ContainsKey(name))
                    col.ExtendedProperties["MariaDbType"] = columnTypes[name];
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
            await EnsureDatabaseExistsAsync();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                UniversalType utype = TypeMapper.FromExtendedProperties(col);
                string mariaType = TypeMapper.ToMariaDb(utype);
                columns.Add($"`{col.ColumnName.Trim()}` {mariaType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName.Trim()}` ({string.Join(", ", columns)})";
            System.Diagnostics.Debug.WriteLine(sql);
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
                var cols = string.Join(", ", data.Columns.Cast<DataColumn>()
                    .Select(c => $"`{c.ColumnName.Trim()}`"));
                var vals = string.Join(", ", data.Columns.Cast<DataColumn>()
                    .Select(c => $"@p_{c.ColumnName.Trim()}"));
                string sql = $"INSERT INTO `{tableName.Trim()}` ({cols}) VALUES ({vals})";
                using var cmd = new MySqlCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                {
                    string safe = DbConverter.ToSafeString(row[col]);
                    if (safe == null)
                    {
                        cmd.Parameters.AddWithValue($"@p_{col.ColumnName.Trim()}", DBNull.Value);
                        continue;
                    }
                    UniversalType utype = TypeMapper.FromExtendedProperties(col);
                    string converted = DbConverter.ConvertToString(safe, utype);
                    cmd.Parameters.AddWithValue($"@p_{col.ColumnName.Trim()}",
                        converted != null ? (object)converted : DBNull.Value);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}