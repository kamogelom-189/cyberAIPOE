using System;
using System.Collections.Generic;
using System.Linq;
using CyberBotWPF.Models;

namespace CyberBotWPF.Managers;

/// <summary>
/// Calculates reminder dates ("today", "tomorrow", "in N days", specific date)
/// and checks the task list for due/overdue reminders at startup.
/// </summary>
public class ReminderManager
{
    private readonly TaskManager _taskManager;

    public ReminderManager(TaskManager taskManager) => _taskManager = taskManager;

    /// <summary>Parses natural phrases like "tomorrow", "in 7 days", "today" into a DateTime.</summary>
    public static DateTime? ParseReminderPhrase(string phrase)
    {
        phrase = phrase.Trim().ToLower();

        if (phrase.Contains("today")) return DateTime.Today;
        if (phrase.Contains("tomorrow")) return DateTime.Today.AddDays(1);

        // "in 7 days" / "7 days" / "in 3 day"
        var match = System.Text.RegularExpressions.Regex.Match(phrase, @"(\d+)\s*day");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
            return DateTime.Today.AddDays(days);

        // Specific date e.g. "2026-07-01" or "01/07/2026"
        if (DateTime.TryParse(phrase, out var specific))
            return specific.Date;

        return null;
    }

    /// <summary>Returns tasks whose reminder is today or earlier and not yet completed — shown as a startup popup.</summary>
    public List<TaskModel> GetDueReminders() => _taskManager.GetDueOrOverdueTasks();
}
