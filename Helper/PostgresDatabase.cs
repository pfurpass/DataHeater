using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace DataHeater.Helper
{
    internal class PostgresDatabase : ITargetDatabase
    {
        private readonly string _connectionString;

        public PostgresDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<string>> GetTablesAsync()
        {
            var tables = new List<string>();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = "SELECT tablename FROM pg_tables WHERE schemaname = 'public';";
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
            return tables;
        }

        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string schemaSql = "SELECT column_name, udt_name FROM information_schema.columns " +
                               "WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position";
            using (var schemaCmd = new NpgsqlCommand(schemaSql, conn))
            {
                schemaCmd.Parameters.AddWithValue("@t", tableName);
                using var schemaReader = await schemaCmd.ExecuteReaderAsync();
                while (await schemaReader.ReadAsync())
                    columnTypes[schemaReader.GetString(0)] = schemaReader.GetString(1).ToUpper();
            }

            using var cmd = new NpgsqlCommand($"SELECT * FROM \"{tableName}\"", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                var col = table.Columns.Add(name, typeof(string));
                if (columnTypes.ContainsKey(name))
                    col.ExtendedProperties["PostgresType"] = columnTypes[name];
            }

            while (await reader.ReadAsync())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i).ToString();
                table.Rows.Add(row);
            }
            return table;
        }

        public async Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                string sourceType =
                    col.ExtendedProperties.Contains("SqliteType") ? col.ExtendedProperties["SqliteType"].ToString().ToUpper() :
                    col.ExtendedProperties.Contains("MariaDbType") ? col.ExtendedProperties["MariaDbType"].ToString().ToUpper() :
                    null;
                string pgType = MapToPostgres(col.DataType, sourceType);
                columns.Add($"\"{col.ColumnName}\" {pgType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(",", columns)})";
            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task TruncateTableAsync(string tableName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand($"TRUNCATE TABLE \"{tableName}\"", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            foreach (DataRow row in data.Rows)
            {
                var cols = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\""));
                var vals = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => $"@{c.ColumnName}"));
                string sql = $"INSERT INTO \"{tableName}\" ({cols}) VALUES ({vals})";
                using var cmd = new NpgsqlCommand(sql, conn);
                foreach (DataColumn col in data.Columns)
                {
                    var val = row[col];
                    if (val == DBNull.Value || val == null)
                    {
                        cmd.Parameters.AddWithValue($"@{col.ColumnName}", DBNull.Value);
                        continue;
                    }

                    string sourceType =
                        col.ExtendedProperties.Contains("SqliteType") ? col.ExtendedProperties["SqliteType"].ToString().ToUpper() :
                        col.ExtendedProperties.Contains("MariaDbType") ? col.ExtendedProperties["MariaDbType"].ToString().ToUpper() :
                        null;

                    if (sourceType != null && sourceType == "DATE")
                    {
                        if (DateTime.TryParse(val.ToString(), out DateTime dt))
                        {
                            var p = new NpgsqlParameter($"@{col.ColumnName}", NpgsqlDbType.Date);
                            p.Value = DateOnly.FromDateTime(dt);
                            cmd.Parameters.Add(p);
                        }
                        else cmd.Parameters.AddWithValue($"@{col.ColumnName}", DBNull.Value);
                        continue;
                    }

                    if (sourceType != null && (sourceType.Contains("DATETIME") || sourceType.Contains("TIMESTAMP")))
                    {
                        if (DateTime.TryParse(val.ToString(), out DateTime dt))
                        {
                            var p = new NpgsqlParameter($"@{col.ColumnName}", NpgsqlDbType.Timestamp);
                            p.Value = dt;
                            cmd.Parameters.Add(p);
                        }
                        else cmd.Parameters.AddWithValue($"@{col.ColumnName}", DBNull.Value);
                        continue;
                    }

                    cmd.Parameters.AddWithValue($"@{col.ColumnName}", val);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private string MapToPostgres(Type type, string sourceType = null)
        {
            if (sourceType != null)
            {
                if (sourceType == "DATE") return "DATE";
                if (sourceType.Contains("DATETIME") || sourceType.Contains("TIMESTAMP")) return "TIMESTAMP";
                if (sourceType.Contains("TIME")) return "TIME";
                if (sourceType.Contains("TINYINT(1)") || sourceType.Contains("BOOL")) return "BOOLEAN";
                if (sourceType.Contains("TINYINT") || sourceType.Contains("SMALLINT")) return "SMALLINT";
                if (sourceType.Contains("MEDIUMINT") || sourceType.Contains("BIGINT")
                                                     || sourceType.Contains("INT")) return "BIGINT";
                if (sourceType.Contains("DOUBLE") || sourceType.Contains("FLOAT")) return "DOUBLE PRECISION";
                if (sourceType.Contains("DECIMAL") || sourceType.Contains("NUMERIC")) return "NUMERIC";
                if (sourceType.Contains("BYTEA") || sourceType.Contains("BLOB")) return "BYTEA";
                if (sourceType.Contains("TEXT")) return "TEXT";
                if (sourceType.StartsWith("VARCHAR")) return sourceType;
                if (sourceType.StartsWith("CHAR")) return sourceType;
                if (!string.IsNullOrWhiteSpace(sourceType)) return sourceType;
            }

            if (type == typeof(long) || type == typeof(int)) return "BIGINT";
            if (type == typeof(double) || type == typeof(float)) return "DOUBLE PRECISION";
            if (type == typeof(decimal)) return "NUMERIC";
            if (type == typeof(bool)) return "BOOLEAN";
            if (type == typeof(DateTime)) return "TIMESTAMP";
            if (type == typeof(byte[])) return "BYTEA";
            return "TEXT";
        }
    }
}