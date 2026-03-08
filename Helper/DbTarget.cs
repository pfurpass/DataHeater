namespace DataHeater.Helper
{
    public enum DbType { SQLite, MariaDB, PostgreSQL, Oracle, CSV, Excel }

    public class DbTarget
    {
        public DbType Type { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string ConnectionString => Type switch
        {
            DbType.SQLite => $"Data Source={Database};",
            DbType.PostgreSQL => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};",
            DbType.Oracle => $"Data Source={Host}:{Port}/{Database};User Id={Username};Password={Password};",
            DbType.CSV => Database,
            DbType.Excel => Database,
            _ => $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};"
        };

        public string ConnectionStringWithoutDb => Type switch
        {
            DbType.PostgreSQL => $"Host={Host};Port={Port};Database=postgres;Username={Username};Password={Password};",
            DbType.Oracle => $"Data Source={Host}:{Port}/XE;User Id={Username};Password={Password};",
            _ => $"Server={Host};Port={Port};Uid={Username};Pwd={Password};"
        };

        public override string ToString() => Type switch
        {
            DbType.SQLite => $"[SQLite] {Database}",
            DbType.PostgreSQL => $"[PostgreSQL] {Host}:{Port}/{Database}",
            DbType.Oracle => $"[Oracle] {Host}:{Port}/{Database}",
            DbType.CSV => $"[CSV] {Database}",
            DbType.Excel => $"[Excel] {Database}",
            _ => $"[MariaDB] {Host}:{Port}/{Database}"
        };
    }
}