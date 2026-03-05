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
                    if (reader.IsDBNull(i)) { row[i] = DBNull.Value; continue; }
                    var rawValue = reader.GetValue(i);
                    if (rawValue is string s && string.IsNullOrWhiteSpace(s)) { row[i] = DBNull.Value; continue; }
                    try { row[i] = Convert.ChangeType(rawValue, table.Columns[i].DataType); }
                    catch { row[i] = DBNull.Value; }
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
                string sourceType =
                    col.ExtendedProperties.Contains("MariaDbType") ? col.ExtendedProperties["MariaDbType"].ToString().ToUpper() :
                    col.ExtendedProperties.Contains("PostgresType") ? col.ExtendedProperties["PostgresType"].ToString().ToUpper() :
                    null;
                string sqliteType = MapToSqlite(col.DataType, sourceType);
                columns.Add($"`{col.ColumnName}` {sqliteType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` ({string.Join(",", columns)})";
            using var cmd = new SqliteCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private string MapToSqlite(Type type, string sourceType = null)
        {
            if (sourceType != null)
            {
                if (sourceType == "DATE") return "DATE";
                if (sourceType.Contains("DATETIME") || sourceType.Contains("TIMESTAMP")) return "DATETIME";
                if (sourceType.Contains("TIME")) return "TIME";
                if (sourceType.Contains("TINYINT(1)") || sourceType.Contains("BOOL")) return "BOOLEAN";
                if (sourceType.Contains("INT")) return "INTEGER";
                if (sourceType.Contains("DOUBLE") || sourceType.Contains("FLOAT")
                                                   || sourceType.Contains("REAL")) return "REAL";
                if (sourceType.Contains("DECIMAL") || sourceType.Contains("NUMERIC")) return "NUMERIC";
                if (sourceType.Contains("BYTEA") || sourceType.Contains("BLOB")) return "BLOB";
                if (sourceType.Contains("TEXT")) return "TEXT";
                if (sourceType.StartsWith("VARCHAR") || sourceType.StartsWith("CHAR")) return sourceType;
                if (!string.IsNullOrWhiteSpace(sourceType)) return sourceType;
            }

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