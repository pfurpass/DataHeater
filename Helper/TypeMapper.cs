namespace DataHeater.Helper
{
    internal static class TypeMapper
    {
        // ── SQLite → Universal ─────────────────────────────────
        public static UniversalType FromSqlite(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return UniversalType.Text;
            t = t.ToUpper().Trim();

            if (t == "DATE") return UniversalType.Date;
            if (t.Contains("DATETIME") || t.Contains("TIMESTAMP")) return UniversalType.DateTime;
            if (t.Contains("TIME")) return UniversalType.Time;
            if (t == "BOOLEAN" || t == "BOOL") return UniversalType.Boolean;
            if (t.Contains("INT")) return UniversalType.BigInteger;
            if (t.Contains("REAL") || t.Contains("FLOAT")
                                    || t.Contains("DOUBLE")) return UniversalType.Float;
            if (t.Contains("DECIMAL") || t.Contains("NUMERIC")) return UniversalType.Decimal;
            if (t.Contains("BLOB")) return UniversalType.Blob;
            return UniversalType.Text;
        }

        // ── MariaDB → Universal ────────────────────────────────
        public static UniversalType FromMariaDb(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return UniversalType.Text;
            t = t.ToUpper().Trim();

            if (t == "DATE") return UniversalType.Date;
            if (t.Contains("DATETIME") || t.Contains("TIMESTAMP")) return UniversalType.DateTime;
            if (t.Contains("TIME")) return UniversalType.Time;
            if (t.Contains("TINYINT(1)") || t == "BOOL"
                                          || t == "BOOLEAN") return UniversalType.Boolean;
            if (t.Contains("TINYINT") || t.Contains("SMALLINT")
                                       || t.Contains("MEDIUMINT")
                                       || t.Contains("INT")) return UniversalType.BigInteger;
            if (t.Contains("DOUBLE") || t.Contains("FLOAT")) return UniversalType.Float;
            if (t.Contains("DECIMAL") || t.Contains("NUMERIC")) return UniversalType.Decimal;
            if (t.Contains("BLOB")) return UniversalType.Blob;
            return UniversalType.Text;
        }

        // ── PostgreSQL → Universal ─────────────────────────────
        public static UniversalType FromPostgres(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return UniversalType.Text;
            t = t.ToUpper().Trim();

            if (t == "DATE") return UniversalType.Date;
            if (t.Contains("TIMESTAMP")) return UniversalType.DateTime;
            if (t.Contains("TIME")) return UniversalType.Time;
            if (t == "BOOL" || t == "BOOLEAN") return UniversalType.Boolean;
            if (t == "INT2" || t == "INT4" || t == "INT8"
                             || t.Contains("INT")) return UniversalType.BigInteger;
            if (t == "FLOAT4" || t == "FLOAT8"
                              || t == "DOUBLE PRECISION") return UniversalType.Float;
            if (t == "NUMERIC" || t.Contains("DECIMAL")) return UniversalType.Decimal;
            if (t == "BYTEA") return UniversalType.Blob;
            return UniversalType.Text;
        }

        // ── Universal → SQLite ─────────────────────────────────
        public static string ToSqlite(UniversalType t) => t switch
        {
            UniversalType.Date => "DATE",
            UniversalType.DateTime => "DATETIME",
            UniversalType.Time => "TIME",
            UniversalType.Boolean => "INTEGER",
            UniversalType.Integer => "INTEGER",
            UniversalType.BigInteger => "INTEGER",
            UniversalType.Float => "REAL",
            UniversalType.Decimal => "NUMERIC",
            UniversalType.Blob => "BLOB",
            _ => "TEXT"
        };

        // ── Universal → MariaDB ────────────────────────────────
        public static string ToMariaDb(UniversalType t) => t switch
        {
            UniversalType.Date => "DATE",
            UniversalType.DateTime => "DATETIME",
            UniversalType.Time => "TIME",
            UniversalType.Boolean => "BOOLEAN",
            UniversalType.Integer => "INT",
            UniversalType.BigInteger => "BIGINT",
            UniversalType.Float => "DOUBLE",
            UniversalType.Decimal => "DECIMAL(18,6)",
            UniversalType.Blob => "LONGBLOB",
            _ => "TEXT"
        };

        // ── Universal → PostgreSQL ─────────────────────────────
        public static string ToPostgres(UniversalType t) => t switch
        {
            UniversalType.Date => "DATE",
            UniversalType.DateTime => "TIMESTAMP",
            UniversalType.Time => "TIME",
            UniversalType.Boolean => "BOOLEAN",
            UniversalType.Integer => "INTEGER",
            UniversalType.BigInteger => "BIGINT",
            UniversalType.Float => "DOUBLE PRECISION",
            UniversalType.Decimal => "NUMERIC",
            UniversalType.Blob => "BYTEA",
            _ => "TEXT"
        };

        // ── Hilfsmethode: ExtendedProperties → UniversalType ──
        public static UniversalType FromExtendedProperties(System.Data.DataColumn col)
        {
            if (col.ExtendedProperties.Contains("SqliteType"))
                return FromSqlite(col.ExtendedProperties["SqliteType"]?.ToString());
            if (col.ExtendedProperties.Contains("MariaDbType"))
                return FromMariaDb(col.ExtendedProperties["MariaDbType"]?.ToString());
            if (col.ExtendedProperties.Contains("PostgresType"))
                return FromPostgres(col.ExtendedProperties["PostgresType"]?.ToString());
            return UniversalType.Text;
        }
    }
}