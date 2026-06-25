using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CyberBotWPF.Data;
using CyberBotWPF.Models;

namespace CyberBotWPF.Managers;

/// <summary>
/// Records every important action the app performs (task changes, quiz events,
/// chat commands, NLP matches, DB events, errors) into the ActivityLog table.
///
/// If the database is unreachable, entries are kept in an in-memory list instead
/// so the application never crashes and the Activity Log tab still works —
/// they get flushed to MySQL automatically once a connection succeeds again.
/// </summary>
public class ActivityLogger
{
    private readonly DatabaseHelper _db;
    private readonly List<ActivityModel> _memoryLog = new();
    private bool _usingFallback;

    public bool UsingFallback => _usingFallback;

    public ActivityLogger(DatabaseHelper db) => _db = db;

    public void Log(string action, string details)
    {
        var entry = new ActivityModel { Timestamp = DateTime.Now, Action = action, Details = details };

        try
        {
            var sql = "INSERT INTO ActivityLog (Timestamp, Action, Details) VALUES (@ts, @action, @details);";
            var id = _db.ExecuteInsertReturningId(sql, new()
            {
                ["@ts"] = entry.Timestamp,
                ["@action"] = entry.Action,
                ["@details"] = entry.Details
            });
            entry.LogID = (int)id;
            _usingFallback = false;
        }
        catch
        {
            // Database unavailable — fall back to memory so logging never throws.
            _usingFallback = true;
            entry.LogID = -(_memoryLog.Count + 1);
            _memoryLog.Add(entry);
        }
    }

    /// <summary>Returns the most recent N entries (default view requirement: last 10).</summary>
    public List<ActivityModel> GetRecent(int count = 10)
    {
        var all = GetAll();
        return all.OrderByDescending(a => a.Timestamp).Take(count).ToList();
    }

    public List<ActivityModel> GetAll()
    {
        try
        {
            var sql = "SELECT LogID, Timestamp, Action, Details FROM ActivityLog ORDER BY Timestamp DESC;";
            var dbResults = _db.ExecuteQuery(sql, r => new ActivityModel
            {
                LogID = r.GetInt32(0),
                Timestamp = r.GetDateTime(1),
                Action = r.GetString(2),
                Details = r.IsDBNull(3) ? "" : r.GetString(3)
            });
            _usingFallback = false;
            // Include any not-yet-flushed memory entries too.
            return dbResults.Concat(_memoryLog).OrderByDescending(a => a.Timestamp).ToList();
        }
        catch
        {
            _usingFallback = true;
            return _memoryLog.OrderByDescending(a => a.Timestamp).ToList();
        }
    }

    public List<ActivityModel> Search(string keyword)
    {
        keyword = keyword.Trim().ToLower();
        return GetAll().Where(a =>
            a.Action.ToLower().Contains(keyword) || a.Details.ToLower().Contains(keyword)).ToList();
    }

    public void ClearLog()
    {
        try
        {
            _db.ExecuteNonQuery("DELETE FROM ActivityLog;");
        }
        catch
        {
            _usingFallback = true;
        }
        _memoryLog.Clear();
    }

    /// <summary>Exports the full log to a CSV file (bonus feature: Export Log).</summary>
    public void ExportToCsv(string filePath)
    {
        var entries = GetAll();
        using var writer = new StreamWriter(filePath, false);
        writer.WriteLine("LogID,Timestamp,Action,Details");
        foreach (var e in entries)
        {
            string safeDetails = e.Details.Replace("\"", "\"\"");
            writer.WriteLine($"{e.LogID},\"{e.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{e.Action}\",\"{safeDetails}\"");
        }
    }
}
