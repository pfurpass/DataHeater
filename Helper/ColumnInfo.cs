namespace DataHeater.Helper
{
    public enum DbDateKind { None, DateOnly, TimeOnly, DateTime }

    public class ColumnInfo
    {
        public string Name { get; set; }
        public System.Type DotNetType { get; set; } = typeof(string);
        public string OriginalDbTypeName { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool Nullable { get; set; } = true;
        public DbDateKind DateKind { get; set; } = DbDateKind.None;
    }
}