using System;

namespace CyberBotWPF.Models;

/// <summary>
/// Represents one row in the Activity Log (either persisted to MySQL
/// "ActivityLog" table, or kept in the in-memory fallback list if the
/// database is unreachable — see ActivityLogger).
/// </summary>
public class ActivityModel
{
    public int LogID { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";

    public string DateText => Timestamp.ToString("yyyy-MM-dd");
    public string TimeText => Timestamp.ToString("HH:mm:ss");

    public override string ToString() => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Action} — {Details}";
}
