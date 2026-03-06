using System;
using System.Globalization;

namespace DataHeater.Helper
{
    internal static class DbConverter
    {
        public static string ToSafeString(object val)
        {
            if (val == null || val == DBNull.Value) return null;
            string s = val.ToString();
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (s.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;
            return s;
        }

        // Für MariaDB/SQLite — gibt bereinigten String zurück
        public static string ConvertToString(string raw, UniversalType utype)
        {
            if (raw == null) return null;

            switch (utype)
            {
                case UniversalType.DateTime:
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime dt))
                        return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                        DateTimeStyles.None, out DateTime dt2))
                        return dt2.ToString("yyyy-MM-dd HH:mm:ss");
                    if (TimeSpan.TryParse(raw, out TimeSpan ts))
                        return DateTime.Today.Add(ts).ToString("yyyy-MM-dd HH:mm:ss");
                    return null;

                case UniversalType.Date:
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime d))
                        return d.ToString("yyyy-MM-dd");
                    if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                        DateTimeStyles.None, out DateTime d2))
                        return d2.ToString("yyyy-MM-dd");
                    if (TimeSpan.TryParse(raw, out _))
                        return DateTime.Today.ToString("yyyy-MM-dd");
                    return null;

                case UniversalType.Time:
                    if (TimeSpan.TryParse(raw, out TimeSpan t))
                        return t.ToString(@"hh\:mm\:ss");
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime dt3))
                        return dt3.TimeOfDay.ToString(@"hh\:mm\:ss");
                    return null;

                case UniversalType.Float:
                    // Komma → Punkt, InvariantCulture
                    string floatStr = raw.Replace(",", ".");
                    if (double.TryParse(floatStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double dbl))
                        return dbl.ToString(CultureInfo.InvariantCulture);
                    return null;

                case UniversalType.Decimal:
                    string decStr = raw.Replace(",", ".");
                    if (decimal.TryParse(decStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal dec))
                        return dec.ToString(CultureInfo.InvariantCulture);
                    return null;

                case UniversalType.Integer:
                case UniversalType.BigInteger:
                    // Vielleicht "1.0" aus SQLite
                    string intStr = raw.Replace(",", ".");
                    if (long.TryParse(intStr, out long l))
                        return l.ToString();
                    if (double.TryParse(intStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d3))
                        return ((long)d3).ToString();
                    return null;

                case UniversalType.Boolean:
                    if (raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase))
                        return "1";
                    if (raw == "0" || raw.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return "0";
                    return null;

                default:
                    return raw;
            }
        }

        // Für PostgreSQL — gibt typisierten Wert zurück
        public static object ConvertForPostgres(string raw, UniversalType utype)
        {
            if (raw == null) return DBNull.Value;

            switch (utype)
            {
                case UniversalType.DateTime:
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime dt)) return dt;
                    if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                        DateTimeStyles.None, out DateTime dt2)) return dt2;
                    if (TimeSpan.TryParse(raw, out TimeSpan ts))
                        return DateTime.Today.Add(ts);
                    return DBNull.Value;

                case UniversalType.Date:
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime d))
                        return DateOnly.FromDateTime(d);
                    if (DateTime.TryParse(raw, CultureInfo.CurrentCulture,
                        DateTimeStyles.None, out DateTime d2))
                        return DateOnly.FromDateTime(d2);
                    if (TimeSpan.TryParse(raw, out _))
                        return DateOnly.FromDateTime(DateTime.Today);
                    return DBNull.Value;

                case UniversalType.Time:
                    if (TimeSpan.TryParse(raw, out TimeSpan t)) return t;
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime dt3))
                        return dt3.TimeOfDay;
                    return DBNull.Value;

                case UniversalType.Float:
                    string floatStr = raw.Replace(",", ".");
                    if (double.TryParse(floatStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double dbl)) return dbl;
                    return DBNull.Value;

                case UniversalType.Decimal:
                    string decStr = raw.Replace(",", ".");
                    if (decimal.TryParse(decStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal dec)) return dec;
                    return DBNull.Value;

                case UniversalType.Integer:
                case UniversalType.BigInteger:
                    string intStr = raw.Replace(",", ".");
                    if (long.TryParse(intStr, out long l)) return l;
                    if (double.TryParse(intStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d3))
                        return (long)d3;
                    return DBNull.Value;

                case UniversalType.Boolean:
                    if (raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (raw == "0" || raw.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return DBNull.Value;

                default:
                    return raw;
            }
        }
    }
}