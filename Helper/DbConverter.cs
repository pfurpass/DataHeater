using System;
using System.Globalization;

namespace DataHeater.Helper
{
    /// <summary>
    /// Regel: Nur echtes DBNull/null aus der Quelle → NULL im Ziel.
    /// Werte die nicht geparst werden können → Rohwert als String durchreichen.
    /// Niemals einen vorhandenen Wert durch NULL ersetzen.
    /// </summary>
    internal static class DbConverter
    {
        // Gibt null zurück NUR wenn Quelle wirklich null/DBNull ist
        public static string ToSafeString(object val)
        {
            if (val == null || val == DBNull.Value) return null;
            return val.ToString(); // leer, "null" usw. → trotzdem durchreichen
        }

        // ── Für MariaDB / SQLite: gibt String zurück ───────────────────────
        // Wenn Konvertierung fehlschlägt → raw zurückgeben, nie null
        public static string ConvertToString(string raw, ColumnInfo info)
        {
            if (raw == null) return null; // Quelle war wirklich NULL

            switch (info.DateKind)
            {
                case DbDateKind.DateOnly:
                    {
                        if (IsNullLike(raw)) return null;
                        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateOnly d))
                            return d.ToString("yyyy-MM-dd");
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime dt))
                            return dt.ToString("yyyy-MM-dd");
                        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                                DateTimeStyles.None, out DateTime dt2))
                            return dt2.ToString("yyyy-MM-dd");
                        return null;
                    }
                case DbDateKind.DateTime:
                    {
                        if (IsNullLike(raw)) return null;
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime dt))
                            return dt.ToString("yyyy-MM-dd HH:mm:ss");
                        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                                DateTimeStyles.None, out DateTime dt2))
                            return dt2.ToString("yyyy-MM-dd HH:mm:ss");
                        if (TimeSpan.TryParse(raw, out TimeSpan ts))
                            return DateTime.Today.Add(ts).ToString("yyyy-MM-dd HH:mm:ss");
                        return null;
                    }
                case DbDateKind.TimeOnly:
                    {
                        if (IsNullLike(raw)) return null;
                        if (TimeSpan.TryParse(raw, out TimeSpan ts))
                            return ts.ToString(@"hh\:mm\:ss");
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime dt))
                            return dt.TimeOfDay.ToString(@"hh\:mm\:ss");
                        return null;
                    }
                default:
                    return ConvertScalar(raw, info);
            }
        }

        // ── Für PostgreSQL: gibt typisierten Wert zurück ───────────────────
        public static object ConvertForPostgres(string raw, ColumnInfo info)
        {
            if (raw == null) return DBNull.Value;

            switch (info.DateKind)
            {
                case DbDateKind.DateOnly:
                    {
                        if (IsNullLike(raw)) return DBNull.Value;
                        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateOnly d))
                            return d;
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime dt))
                            return DateOnly.FromDateTime(dt);
                        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                                DateTimeStyles.None, out DateTime dt2))
                            return DateOnly.FromDateTime(dt2);
                        return DBNull.Value;
                    }
                case DbDateKind.DateTime:
                    {
                        if (IsNullLike(raw)) return DBNull.Value;
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime dt))
                            return dt;
                        if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                                DateTimeStyles.None, out DateTime dt2))
                            return dt2;
                        if (TimeSpan.TryParse(raw, out TimeSpan ts))
                            return DateTime.Today.Add(ts);
                        return DBNull.Value;
                    }
                case DbDateKind.TimeOnly:
                    {
                        if (IsNullLike(raw)) return DBNull.Value;
                        if (TimeSpan.TryParse(raw, out TimeSpan ts))
                            return ts;
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime dt))
                            return dt.TimeOfDay;
                        return DBNull.Value;
                    }
                default:
                    // Wenn ConvertScalarTyped null zurückgibt (IsNullLike oder Parse-Fehler)
                    // → DBNull, NIEMALS den Raw-String durchreichen (würde z.B. "null" an BIGINT schicken)
                    var typed = ConvertScalarTyped(raw, info);
                    return typed ?? DBNull.Value;
            }
        }

        // Ist der String für typisierte Spalten semantisch NULL?
        // ("null", leer, nur Leerzeichen) → true
        private static bool IsNullLike(string raw)
            => string.IsNullOrWhiteSpace(raw)
            || raw.Equals("null", StringComparison.OrdinalIgnoreCase);

        // ── Skalare Typen als String ───────────────────────────────────────
        private static string ConvertScalar(string raw, ColumnInfo info)
        {
            // String-Spalten: Rohwert immer behalten (auch "null" als Text)
            if (info.DotNetType == typeof(string)) return raw;

            // Typisierte Spalten: "null"/leer → echtes NULL
            if (IsNullLike(raw)) return null;

            if (info.DotNetType == typeof(double))
            {
                string f = raw.Replace(",", ".");
                if (double.TryParse(f, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d))
                    return d.ToString(CultureInfo.InvariantCulture);
                return null; // kein gültiger Wert → NULL statt kaputten String
            }
            if (info.DotNetType == typeof(decimal))
            {
                string f = raw.Replace(",", ".");
                if (decimal.TryParse(f, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal d))
                    return d.ToString(CultureInfo.InvariantCulture);
                return null;
            }
            if (info.DotNetType == typeof(long) || info.DotNetType == typeof(int))
            {
                string f = raw.Replace(",", ".");
                if (long.TryParse(f, out long l)) return l.ToString();
                if (double.TryParse(f, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d))
                    return ((long)d).ToString();
                return null;
            }
            if (info.DotNetType == typeof(bool))
            {
                if (raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase)) return "1";
                if (raw == "0" || raw.Equals("false", StringComparison.OrdinalIgnoreCase)) return "0";
                return null;
            }
            return raw;
        }

        // ── Skalare Typen als typisiertes Objekt ──────────────────────────
        private static object ConvertScalarTyped(string raw, ColumnInfo info)
        {
            if (info.DotNetType == typeof(string)) return raw;
            if (IsNullLike(raw)) return null;

            if (info.DotNetType == typeof(double))
            {
                string f = raw.Replace(",", ".");
                if (double.TryParse(f, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d)) return d;
                return null;
            }
            if (info.DotNetType == typeof(decimal))
            {
                string f = raw.Replace(",", ".");
                if (decimal.TryParse(f, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal d)) return d;
                return null;
            }
            if (info.DotNetType == typeof(long) || info.DotNetType == typeof(int))
            {
                string f = raw.Replace(",", ".");
                if (long.TryParse(f, out long l)) return l;
                if (double.TryParse(f, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d))
                    return (long)d;
                return null;
            }
            if (info.DotNetType == typeof(bool))
            {
                if (raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (raw == "0" || raw.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
                return null;
            }
            return raw;
        }
    }
}