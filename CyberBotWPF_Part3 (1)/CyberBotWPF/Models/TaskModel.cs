using System;

namespace CyberBotWPF.Models;

/// <summary>
/// Represents a single cybersecurity task / reminder row.
/// Mirrors the "Tasks" table in MySQL exactly (see Database/schema.sql).
/// </summary>
public class TaskModel
{
    public int TaskID { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Optional reminder date. Null = no reminder set.</summary>
    public DateTime? ReminderDate { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.Now;

    /// <summary>True if a reminder exists and the date is today or in the past.</summary>
    public bool IsReminderDue =>
        ReminderDate.HasValue && !IsCompleted && ReminderDate.Value.Date <= DateTime.Today;

    /// <summary>Friendly display string used in the chatbot and status messages.</summary>
    public string ToChatSummary()
    {
        string status = IsCompleted ? "✅ Completed" : "🕒 Pending";
        string reminder = ReminderDate.HasValue ? $" | Reminder: {ReminderDate:dd MMM yyyy}" : "";
        return $"#{TaskID} \"{Title}\" — {status}{reminder}";
    }
}
