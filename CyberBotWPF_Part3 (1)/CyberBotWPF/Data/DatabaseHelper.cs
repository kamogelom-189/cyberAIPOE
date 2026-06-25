using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;

namespace CyberBotWPF.Data;

/// <summary>
/// Reusable, low-level MySQL access helper. Every query in the project goes
/// through here so that:
///   1. All SQL is parameterized (no string concatenation / no SQL injection).
///   2. Connection lifetime + exception handling is centralised in one place.
///   3. Higher-level managers (TaskManager, ActivityLogger, ReminderManager)
///      stay free of raw ADO.NET code.
/// </summary>
public class DatabaseHelper
{
    /// <summary>Raised whenever a database operation fails, so the UI / ActivityLogger can react.</summary>
    public event Action<string>? OnDatabaseError;

    private string ConnectionString => DbConfig.BuildConnectionString();

    /// <summary>Quick connectivity check used by the "Test Connection" button.</summary>
    public bool TestConnection(out string message)
    {
        try
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            message = $"✅ Connected to MySQL successfully (server: {DbConfig.Server}, db: {DbConfig.Database}).";
            return true;
        }
        catch (Exception ex)
        {
            message = $"❌ Connection failed: {ex.Message}";
            OnDatabaseError?.Invoke(message);
            return false;
        }
    }

    /// <summary>
    /// Creates the Tasks and ActivityLog tables if they do not already exist.
    /// Safe to call every startup — uses CREATE TABLE IF NOT EXISTS.
    /// </summary>
    public bool InitializeSchema(out string message)
    {
        const string tasksSql = @"
            CREATE TABLE IF NOT EXISTS Tasks (
                TaskID        INT AUTO_INCREMENT PRIMARY KEY,
                Title         VARCHAR(200)  NOT NULL,
                Description   TEXT          NULL,
                ReminderDate  DATETIME      NULL,
                IsCompleted   BOOLEAN       NOT NULL DEFAULT 0,
                DateCreated   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";

        const string logSql = @"
            CREATE TABLE IF NOT EXISTS ActivityLog (
                LogID      INT AUTO_INCREMENT PRIMARY KEY,
                Timestamp  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Action     VARCHAR(100) NOT NULL,
                Details    VARCHAR(500) NULL
            );";

        try
        {
            ExecuteNonQuery(tasksSql);
            ExecuteNonQuery(logSql);
            message = "✅ Schema verified / created successfully (Tasks, ActivityLog).";
            return true;
        }
        catch (Exception ex)
        {
            message = $"❌ Schema initialization failed: {ex.Message}";
            OnDatabaseError?.Invoke(message);
            return false;
        }
    }

    /// <summary>Executes INSERT / UPDATE / DELETE / DDL. Returns affected row count, or -1 on AUTO_INCREMENT inserts (see ExecuteInsertReturningId).</summary>
    public int ExecuteNonQuery(string sql, Dictionary<string, object?>? parameters = null)
    {
        using var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Executes an INSERT and returns the new AUTO_INCREMENT TaskID/LogID.</summary>
    public long ExecuteInsertReturningId(string sql, Dictionary<string, object?>? parameters = null)
    {
        using var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        cmd.ExecuteNonQuery();
        return cmd.LastInsertedId;
    }

    /// <summary>Executes a SELECT and maps each row via the supplied projection function.</summary>
    public List<T> ExecuteQuery<T>(string sql, Func<IDataReader, T> map, Dictionary<string, object?>? parameters = null)
    {
        var results = new List<T>();
        using var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            results.Add(map(reader));
        return results;
    }

    private static void AddParameters(MySqlCommand cmd, Dictionary<string, object?>? parameters)
    {
        if (parameters is null) return;
        foreach (var (key, value) in parameters)
            cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
    }
}
