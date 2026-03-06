namespace DataHeater.Helper
{
    public enum DbType { SQLite, MariaDB, PostgreSQL }

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
            _ => $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};"
        };

        // Verbindung ohne Datenbankname — zum Erstellen der DB
        public string ConnectionStringWithoutDb => Type switch
        {
            DbType.PostgreSQL => $"Host={Host};Port={Port};Database=postgres;Username={Username};Password={Password};",
            _ => $"Server={Host};Port={Port};Uid={Username};Pwd={Password};"
        };

        public override string ToString() => Type switch
        {
            DbType.SQLite => $"[SQLite] {Database}",
            DbType.PostgreSQL => $"[PostgreSQL] {Host}:{Port}/{Database}",
            _ => $"[MariaDB] {Host}:{Port}/{Database}"
        };
    }
}