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
        private readonly string _connectionStringWithoutDb;
        private readonly string _databaseName;

        public PostgresDatabase(string connectionString, string connectionStringWithoutDb, string databaseName)
        {
            _connectionString = connectionString;
            _connectionStringWithoutDb = connectionStringWithoutDb;
            _databaseName = databaseName;
        }

        private async Task EnsureDatabaseExistsAsync()
        {
            using var conn = new NpgsqlConnection(_connectionStringWithoutDb);
            await conn.OpenAsync();

            // PostgreSQL: CREATE DATABASE IF NOT EXISTS gibt es nicht — erst prüfen
            using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @name", conn);
            checkCmd.Parameters.AddWithValue("@name", _databaseName.ToLower());
            var exists = await checkCmd.ExecuteScalarAsync();

            if (exists == null)
            {
                using var createCmd = new NpgsqlCommand(
                    $"CREATE DATABASE \"{_databaseName}\" ENCODING 'UTF8';", conn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> GetTablesAsync()
        {
            await EnsureDatabaseExistsAsync();
            var tables = new List<string>();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT tablename FROM pg_tables WHERE schemaname = 'public';", conn);
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
            using (var schemaCmd = new NpgsqlCommand(
                "SELECT column_name, udt_name FROM information_schema.columns " +
                "WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position", conn))
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
                var col = new DataColumn(name, typeof(string));
                if (columnTypes.ContainsKey(name))
                    col.ExtendedProperties["PostgresType"] = columnTypes[name];
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
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var columns = new List<string>();
            foreach (DataColumn col in schema.Columns)
            {
                UniversalType utype = TypeMapper.FromExtendedProperties(col);
                string pgType = TypeMapper.ToPostgres(utype);
                columns.Add($"\"{col.ColumnName}\" {pgType}");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", columns)})";
            System.Diagnostics.Debug.WriteLine(sql);
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
                var cols = string.Join(", ", data.Columns.Cast<DataColumn>()
                    .Select(c => $"\"{c.ColumnName}\""));
                var vals = string.Join(", ", data.Columns.Cast<DataColumn>()
                    .Select(c => $"@p_{c.ColumnName}"));
                string sql = $"INSERT INTO \"{tableName}\" ({cols}) VALUES ({vals})";
                using var cmd = new NpgsqlCommand(sql, conn);

                foreach (DataColumn col in data.Columns)
                {
                    string safe = DbConverter.ToSafeString(row[col]);
                    if (safe == null)
                    {
                        cmd.Parameters.AddWithValue($"@p_{col.ColumnName}", DBNull.Value);
                        continue;
                    }

                    UniversalType utype = TypeMapper.FromExtendedProperties(col);
                    object converted = DbConverter.ConvertForPostgres(safe, utype);

                    switch (utype)
                    {
                        case UniversalType.Date:
                            var pd = new NpgsqlParameter($"@p_{col.ColumnName}", NpgsqlDbType.Date);
                            pd.Value = converted is DBNull ? DBNull.Value : converted;
                            cmd.Parameters.Add(pd);
                            break;
                        case UniversalType.DateTime:
                            var pdt = new NpgsqlParameter($"@p_{col.ColumnName}", NpgsqlDbType.Timestamp);
                            pdt.Value = converted is DBNull ? DBNull.Value : converted;
                            cmd.Parameters.Add(pdt);
                            break;
                        case UniversalType.Time:
                            var pt = new NpgsqlParameter($"@p_{col.ColumnName}", NpgsqlDbType.Time);
                            pt.Value = converted is DBNull ? DBNull.Value : converted;
                            cmd.Parameters.Add(pt);
                            break;
                        default:
                            cmd.Parameters.AddWithValue($"@p_{col.ColumnName}",
                                converted is DBNull ? DBNull.Value : converted);
                            break;
                    }
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}