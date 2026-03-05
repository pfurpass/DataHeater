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
            string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
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

            var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var pragmaCmd = new SqliteCommand($"PRAGMA table_info(`{tableName}`)", conn))
            using (var pragmaReader = await pragmaCmd.ExecuteReaderAsync())
            {
                while (await pragmaReader.ReadAsync())
                {
                    string colName = pragmaReader.GetString(1);
                    string colType = pragmaReader.GetString(2).ToUpper();
                    columnTypes[colName] = colType;
                }
            }

            using var cmd = new SqliteCommand($"SELECT * FROM `{tableName}`", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                string sqliteType = columnTypes.ContainsKey(name) ? columnTypes[name] : "TEXT";
                Type dotNetType = MapSqliteTypeToDotNet(sqliteType);
                var col = new DataColumn(name, dotNetType);
                col.ExtendedProperties["SqliteType"] = sqliteType;
                table.Columns.Add(col);
            }

            while (await reader.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.IsDBNull(i))
                    {
                        row[i] = DBNull.Value;
                        continue;
                    }

                    var rawValue = reader.GetValue(i);

                    if (rawValue is string s && string.IsNullOrWhiteSpace(s))
                    {
                        row[i] = DBNull.Value;
                        continue;
                    }

                    try
                    {
                        row[i] = Convert.ChangeType(rawValue, table.Columns[i].DataType);
                    }
                    catch
                    {
                        row[i] = DBNull.Value;
                    }
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private Type MapSqliteTypeToDotNet(string sqliteType)
        {
            if (sqliteType.Contains("INT")) return typeof(long);
            if (sqliteType.Contains("REAL") || sqliteType.Contains("FLOAT")
                                             || sqliteType.Contains("DOUBLE")) return typeof(double);
            if (sqliteType.Contains("NUMERIC") || sqliteType.Contains("DECIMAL")) return typeof(decimal);
            if (sqliteType.Contains("BOOL")) return typeof(bool);
            if (sqliteType.Contains("DATE") || sqliteType.Contains("TIME")) return typeof(string);
            if (sqliteType.Contains("BLOB")) return typeof(byte[]);
            return typeof(string);
        }

        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                string mariaDbType = col.ExtendedProperties.Contains("MariaDbType")
                    ? col.ExtendedProperties["MariaDbType"].ToString().ToUpper()
                    : null;
                string sqliteType = MapMariaDbToSqlite(col.DataType, mariaDbType);
                columns.Add($"`{col.ColumnName}` {sqliteType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(",", columns)})";
            using var cmd = new SqliteCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private string MapMariaDbToSqlite(Type type, string mariaDbType = null)
        {
            if (mariaDbType != null)
            {
                if (mariaDbType == "DATE") return "DATE";
                if (mariaDbType.Contains("DATETIME")) return "DATETIME";
                if (mariaDbType.Contains("TIMESTAMP")) return "DATETIME";
                if (mariaDbType.Contains("TIME")) return "TIME";
                if (mariaDbType.Contains("TINYINT(1)")) return "BOOLEAN";
                if (mariaDbType.Contains("TINYINT")) return "INTEGER";
                if (mariaDbType.Contains("SMALLINT")) return "INTEGER";
                if (mariaDbType.Contains("MEDIUMINT")) return "INTEGER";
                if (mariaDbType.Contains("BIGINT")) return "INTEGER";
                if (mariaDbType.Contains("INT")) return "INTEGER";
                if (mariaDbType.Contains("DOUBLE") || mariaDbType.Contains("FLOAT")) return "REAL";
                if (mariaDbType.Contains("DECIMAL") || mariaDbType.Contains("NUMERIC")) return "NUMERIC";
                if (mariaDbType.Contains("BOOL")) return "BOOLEAN";
                if (mariaDbType.Contains("LONGBLOB") || mariaDbType.Contains("BLOB")) return "BLOB";
                if (mariaDbType.Contains("LONGTEXT")) return "TEXT";
                if (mariaDbType.Contains("MEDIUMTEXT")) return "TEXT";
                if (mariaDbType.Contains("TINYTEXT")) return "TEXT";
                if (mariaDbType.Contains("TEXT")) return "TEXT";
                if (mariaDbType.Contains("VARCHAR")) return mariaDbType;
                if (mariaDbType.Contains("CHAR")) return mariaDbType;
                if (mariaDbType.Contains("ENUM")) return "TEXT";
                if (mariaDbType.Contains("SET")) return "TEXT";
                if (mariaDbType.Contains("JSON")) return "TEXT";

                // Unbekannter Typ → direkt übernehmen
                if (!string.IsNullOrWhiteSpace(mariaDbType)) return mariaDbType;
            }

            // Fallback: .NET-Typ
            if (type == typeof(long) || type == typeof(int)) return "INTEGER";
            if (type == typeof(double) || type == typeof(float)) return "REAL";
            if (type == typeof(decimal)) return "NUMERIC";
            if (type == typeof(bool)) return "BOOLEAN";
            if (type == typeof(DateTime)) return "DATETIME";
            if (type == typeof(byte[])) return "BLOB";
            return "TEXT";
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