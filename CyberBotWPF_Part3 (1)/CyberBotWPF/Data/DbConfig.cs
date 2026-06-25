namespace CyberBotWPF.Data;

/// <summary>
/// Simple mutable holder for MySQL connection settings.
/// Edited from the "Database" tab in MainWindow at runtime, so the
/// project never needs a hard-coded connection string.
/// </summary>
public static class DbConfig
{
    public static string Server { get; set; } = "localhost";
    public static string Port { get; set; } = "3306";
    public static string Database { get; set; } = "cyberbot_db";
    public static string User { get; set; } = "root";
    public static string Password { get; set; } = "";

    public static string BuildConnectionString() =>
        $"Server={Server};Port={Port};Database={Database};Uid={User};Pwd={Password};SslMode=none;";
}
