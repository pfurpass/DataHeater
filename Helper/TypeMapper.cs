using System;
using System.Data;

namespace DataHeater.Helper
{
    internal static class TypeMapper
    {
        // ── SQLite ─────────────────────────────────────────────────────────
        public static ColumnInfo FromSqlite(string colName, string dbType)
        {
            var info = new ColumnInfo { Name = colName, OriginalDbTypeName = dbType };
            string t = (dbType ?? "").ToUpperInvariant().Trim();

            if (t == "DATE") { info.DotNetType = typeof(DateOnly); info.DateKind = DbDateKind.DateOnly; }
            else if (t.Contains("DATETIME") || t.Contains("TIMESTAMP")) { info.DotNetType = typeof(DateTime); info.DateKind = DbDateKind.DateTime; }
            else if (t.Contains("TIME")) { info.DotNetType = typeof(TimeSpan); info.DateKind = DbDateKind.TimeOnly; }
            else if (t is "BOOLEAN" or "BOOL") info.DotNetType = typeof(bool);
            else if (t.Contains("INT")) info.DotNetType = typeof(long);
            else if (t.Contains("REAL") || t.Contains("FLOAT") || t.Contains("DOUBLE")) info.DotNetType = typeof(double);
            else if (t.Contains("NUMERIC") || t.Contains("DECIMAL")) info.DotNetType = typeof(decimal);
            else if (t.Contains("BLOB")) info.DotNetType = typeof(byte[]);
            else info.DotNetType = typeof(string);
            return info;
        }

        // ── MariaDB ────────────────────────────────────────────────────────
        public static ColumnInfo FromMariaDb(string colName, string dbType)
        {
            var info = new ColumnInfo { Name = colName, OriginalDbTypeName = dbType };
            string t = (dbType ?? "").ToUpperInvariant().Trim();

            if (t == "DATE") { info.DotNetType = typeof(DateOnly); info.DateKind = DbDateKind.DateOnly; }
            else if (t.Contains("DATETIME") || t.Contains("TIMESTAMP")) { info.DotNetType = typeof(DateTime); info.DateKind = DbDateKind.DateTime; }
            else if (t.Contains("TIME")) { info.DotNetType = typeof(TimeSpan); info.DateKind = DbDateKind.TimeOnly; }
            else if (t.Contains("TINYINT(1)") || t is "BOOL" or "BOOLEAN") info.DotNetType = typeof(bool);
            else if (t.Contains("INT")) info.DotNetType = typeof(long);
            else if (t.Contains("DOUBLE") || t.Contains("FLOAT")) info.DotNetType = typeof(double);
            else if (t.Contains("DECIMAL") || t.Contains("NUMERIC"))
            {
                info.DotNetType = typeof(decimal);
                ParsePrecisionScale(t, out int p, out int s);
                info.Precision = p > 0 ? p : 18;
                info.Scale = s;
            }
            else if (t.Contains("BLOB")) info.DotNetType = typeof(byte[]);
            else info.DotNetType = typeof(string);
            return info;
        }

        // ── PostgreSQL ─────────────────────────────────────────────────────
        public static ColumnInfo FromPostgres(string colName, string dbType)
        {
            var info = new ColumnInfo { Name = colName, OriginalDbTypeName = dbType };
            string t = (dbType ?? "").ToUpperInvariant().Trim();

            if (t == "DATE") { info.DotNetType = typeof(DateOnly); info.DateKind = DbDateKind.DateOnly; }
            else if (t.Contains("TIMESTAMP")) { info.DotNetType = typeof(DateTime); info.DateKind = DbDateKind.DateTime; }
            else if (t.Contains("TIME")) { info.DotNetType = typeof(TimeSpan); info.DateKind = DbDateKind.TimeOnly; }
            else if (t is "BOOL" or "BOOLEAN") info.DotNetType = typeof(bool);
            else if (t is "INT2" or "INT4" or "INT8" || t.Contains("INT")) info.DotNetType = typeof(long);
            else if (t is "FLOAT4" or "FLOAT8" || t.Contains("FLOAT") || t.Contains("DOUBLE")) info.DotNetType = typeof(double);
            else if (t is "NUMERIC" || t.Contains("DECIMAL")) info.DotNetType = typeof(decimal);
            else if (t == "BYTEA") info.DotNetType = typeof(byte[]);
            else info.DotNetType = typeof(string);
            return info;
        }

        // ── Oracle ─────────────────────────────────────────────────────────
        // Oracle DATE enthält immer Zeit; wir erkennen am Spaltennamen ob
        // es nur ein Datum ist (birthday, datefrom, dateuntil …).
        public static ColumnInfo FromOracle(string colName, string dbType)
        {
            var info = new ColumnInfo { Name = colName, OriginalDbTypeName = dbType };
            string t = (dbType ?? "").ToUpperInvariant().Trim();
            string lower = (colName ?? "").ToLowerInvariant().Trim();

            if (t == "DATE")
            {
                bool looksDateOnly =
                    lower.StartsWith("date")
                    || lower.EndsWith("_date")
                    || lower.Contains("_date_")
                    || lower == "birthday" || lower.EndsWith("birthday")
                    || lower.EndsWith("datum") || lower.StartsWith("datum");

                info.DotNetType = typeof(DateTime);
                info.DateKind = looksDateOnly ? DbDateKind.DateOnly : DbDateKind.DateTime;
            }
            else if (t.Contains("TIMESTAMP"))
            {
                info.DotNetType = typeof(DateTime);
                info.DateKind = DbDateKind.DateTime;
            }
            else if (t == "VARCHAR2(8)" || t == "CHAR(8)")
            {
                info.DotNetType = typeof(TimeSpan);
                info.DateKind = DbDateKind.TimeOnly;
            }
            else if (t.Contains("BINARY_FLOAT") || t.Contains("BINARY_DOUBLE") || t.Contains("FLOAT"))
            {
                info.DotNetType = typeof(double);
            }
            else if (t.Contains("NUMBER") || t.Contains("INTEGER") || t.Contains("INT"))
            {
                ParsePrecisionScale(t, out int p, out int s);
                info.Precision = p;
                info.Scale = s;
                info.DotNetType = (p == 1 && s == 0) ? typeof(bool)
                                : (s > 0) ? typeof(decimal)
                                : typeof(long);
            }
            else if (t is "BLOB" or "RAW" or "LONG RAW")
                info.DotNetType = typeof(byte[]);
            else
                info.DotNetType = typeof(string);

            return info;
        }

        // ── ColumnInfo aus DataColumn ExtendedProperties ───────────────────
        public static ColumnInfo FromDataColumn(DataColumn col)
        {
            if (col.ExtendedProperties.Contains("ColumnInfo"))
                return (ColumnInfo)col.ExtendedProperties["ColumnInfo"];
            return new ColumnInfo { Name = col.ColumnName, DotNetType = typeof(string) };
        }

        // ── → SQLite ───────────────────────────────────────────────────────
        public static string ToSqlite(ColumnInfo i)
        {
            if (i.DateKind == DbDateKind.DateOnly) return "DATE";
            if (i.DateKind == DbDateKind.DateTime) return "DATETIME";
            if (i.DateKind == DbDateKind.TimeOnly) return "TIME";
            if (i.DotNetType == typeof(bool)) return "INTEGER";
            if (i.DotNetType == typeof(long)) return "INTEGER";
            if (i.DotNetType == typeof(int)) return "INTEGER";
            if (i.DotNetType == typeof(double)) return "REAL";
            if (i.DotNetType == typeof(decimal)) return "NUMERIC";
            if (i.DotNetType == typeof(byte[])) return "BLOB";
            return "TEXT";
        }

        // ── → MariaDB ──────────────────────────────────────────────────────
        public static string ToMariaDb(ColumnInfo i)
        {
            if (i.DateKind == DbDateKind.DateOnly) return "DATE";
            if (i.DateKind == DbDateKind.DateTime) return "DATETIME";
            if (i.DateKind == DbDateKind.TimeOnly) return "TIME";
            if (i.DotNetType == typeof(bool)) return "BOOLEAN";
            if (i.DotNetType == typeof(long)) return "BIGINT";
            if (i.DotNetType == typeof(int)) return "INT";
            if (i.DotNetType == typeof(double)) return "DOUBLE";
            if (i.DotNetType == typeof(decimal)) return $"DECIMAL({i.Precision ?? 18},{i.Scale ?? 6})";
            if (i.DotNetType == typeof(byte[])) return "LONGBLOB";
            return "TEXT";
        }

        // ── → PostgreSQL ───────────────────────────────────────────────────
        public static string ToPostgres(ColumnInfo i)
        {
            if (i.DateKind == DbDateKind.DateOnly) return "DATE";
            if (i.DateKind == DbDateKind.DateTime) return "TIMESTAMP";
            if (i.DateKind == DbDateKind.TimeOnly) return "TIME";
            if (i.DotNetType == typeof(bool)) return "BOOLEAN";
            if (i.DotNetType == typeof(long)) return "BIGINT";
            if (i.DotNetType == typeof(int)) return "INTEGER";
            if (i.DotNetType == typeof(double)) return "DOUBLE PRECISION";
            if (i.DotNetType == typeof(decimal)) return "NUMERIC";
            if (i.DotNetType == typeof(byte[])) return "BYTEA";
            return "TEXT";
        }

        // ── → Oracle ───────────────────────────────────────────────────────
        public static string ToOracle(ColumnInfo i)
        {
            if (i.DateKind == DbDateKind.DateOnly) return "DATE";
            if (i.DateKind == DbDateKind.DateTime) return "TIMESTAMP";
            if (i.DateKind == DbDateKind.TimeOnly) return "VARCHAR2(8)";
            if (i.DotNetType == typeof(bool)) return "NUMBER(1,0)";
            if (i.DotNetType == typeof(long)) return "NUMBER(19,0)";
            if (i.DotNetType == typeof(int)) return "NUMBER(10,0)";
            if (i.DotNetType == typeof(double)) return "BINARY_DOUBLE";
            if (i.DotNetType == typeof(decimal)) return $"NUMBER({i.Precision ?? 18},{i.Scale ?? 6})";
            if (i.DotNetType == typeof(byte[])) return "BLOB";
            return "NVARCHAR2(2000)";
        }

        // ── Hilfe: "DECIMAL(18,6)" → precision=18, scale=6 ────────────────
        internal static void ParsePrecisionScale(string t, out int precision, out int scale)
        {
            precision = 0; scale = 0;
            int s = t.IndexOf('('), e = t.IndexOf(')');
            if (s < 0 || e < 0) return;
            var parts = t.Substring(s + 1, e - s - 1).Split(',');
            if (parts.Length >= 1) int.TryParse(parts[0].Trim(), out precision);
            if (parts.Length >= 2) int.TryParse(parts[1].Trim(), out scale);
        }
    }
}