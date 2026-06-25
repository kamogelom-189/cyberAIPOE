using System;
using System.Collections.Generic;
using System.Linq;
using CyberBotWPF.Data;
using CyberBotWPF.Models;

namespace CyberBotWPF.Managers;

/// <summary>
/// Handles all CRUD operations for cybersecurity tasks (the "Cybersecurity
/// Task Assistant"). Talks to MySQL exclusively through DatabaseHelper using
/// parameterized queries. Falls back to an in-memory list if the database
/// is unreachable, so the GUI / chatbot keep working and the app never crashes.
/// </summary>
public class TaskManager
{
    private readonly DatabaseHelper _db;
    private readonly ActivityLogger _logger;
    private readonly List<TaskModel> _memoryTasks = new();
    private int _memoryNextId = 1;

    public bool UsingFallback { get; private set; }

    public TaskManager(DatabaseHelper db, ActivityLogger logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── CREATE ──────────────────────────────────────────────────────────────
    public TaskModel AddTask(string title, string description, DateTime? reminderDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title cannot be empty.");

        // Duplicate-task guard (same title, not completed).
        if (GetAllTasks().Any(t => !t.IsCompleted && t.Title.Trim().Equals(title.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A pending task named \"{title}\" already exists.");

        var task = new TaskModel
        {
            Title = title.Trim(),
            Description = description?.Trim() ?? "",
            ReminderDate = reminderDate,
            IsCompleted = false,
            DateCreated = DateTime.Now
        };

        try
        {
            const string sql = @"INSERT INTO Tasks (Title, Description, ReminderDate, IsCompleted, DateCreated)
                                  VALUES (@title, @desc, @reminder, @completed, @created);";
            var id = _db.ExecuteInsertReturningId(sql, new()
            {
                ["@title"] = task.Title,
                ["@desc"] = task.Description,
                ["@reminder"] = task.ReminderDate,
                ["@completed"] = task.IsCompleted,
                ["@created"] = task.DateCreated
            });
            task.TaskID = (int)id;
            UsingFallback = false;
        }
        catch (Exception ex)
        {
            UsingFallback = true;
            task.TaskID = _memoryNextId++;
            _memoryTasks.Add(task);
            _logger.Log("Database Event", $"Insert failed, using memory fallback: {ex.Message}");
        }

        _logger.Log("Task Added", task.ToChatSummary());
        if (reminderDate.HasValue)
            _logger.Log("Reminder Created", $"Task #{task.TaskID} reminder set for {reminderDate:dd MMM yyyy}");

        return task;
    }

    // ── READ ────────────────────────────────────────────────────────────────
    public List<TaskModel> GetAllTasks()
    {
        try
        {
            const string sql = @"SELECT TaskID, Title, Description, ReminderDate, IsCompleted, DateCreated
                                  FROM Tasks ORDER BY IsCompleted ASC, ReminderDate IS NULL, ReminderDate ASC, DateCreated DESC;";
            var results = _db.ExecuteQuery(sql, r => new TaskModel
            {
                TaskID = r.GetInt32(0),
                Title = r.GetString(1),
                Description = r.IsDBNull(2) ? "" : r.GetString(2),
                ReminderDate = r.IsDBNull(3) ? null : r.GetDateTime(3),
                IsCompleted = r.GetBoolean(4),
                DateCreated = r.GetDateTime(5)
            });
            UsingFallback = false;
            return results;
        }
        catch
        {
            UsingFallback = true;
            return _memoryTasks
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.ReminderDate ?? DateTime.MaxValue)
                .ToList();
        }
    }

    public List<TaskModel> SearchTasks(string keyword)
    {
        keyword = keyword.Trim().ToLower();
        return GetAllTasks().Where(t =>
            t.Title.ToLower().Contains(keyword) || t.Description.ToLower().Contains(keyword)).ToList();
    }

    public List<TaskModel> GetDueOrOverdueTasks() =>
        GetAllTasks().Where(t => t.IsReminderDue).ToList();

    // ── UPDATE ──────────────────────────────────────────────────────────────
    public void UpdateTask(TaskModel task)
    {
        try
        {
            const string sql = @"UPDATE Tasks SET Title=@title, Description=@desc, ReminderDate=@reminder, IsCompleted=@completed
                                  WHERE TaskID=@id;";
            _db.ExecuteNonQuery(sql, new()
            {
                ["@title"] = task.Title,
                ["@desc"] = task.Description,
                ["@reminder"] = task.ReminderDate,
                ["@completed"] = task.IsCompleted,
                ["@id"] = task.TaskID
            });
            UsingFallback = false;
        }
        catch
        {
            UsingFallback = true;
            var existing = _memoryTasks.FirstOrDefault(t => t.TaskID == task.TaskID);
            if (existing != null)
            {
                existing.Title = task.Title;
                existing.Description = task.Description;
                existing.ReminderDate = task.ReminderDate;
                existing.IsCompleted = task.IsCompleted;
            }
        }

        _logger.Log("Task Updated", task.ToChatSummary());
    }

    public void MarkComplete(int taskId)
    {
        var task = GetAllTasks().FirstOrDefault(t => t.TaskID == taskId);
        if (task is null) throw new InvalidOperationException($"Task #{taskId} not found.");
        task.IsCompleted = true;
        UpdateTask(task);
        _logger.Log("Task Completed", task.ToChatSummary());
    }

    // ── DELETE ──────────────────────────────────────────────────────────────
    public void DeleteTask(int taskId)
    {
        try
        {
            _db.ExecuteNonQuery("DELETE FROM Tasks WHERE TaskID=@id;", new() { ["@id"] = taskId });
            UsingFallback = false;
        }
        catch
        {
            UsingFallback = true;
            _memoryTasks.RemoveAll(t => t.TaskID == taskId);
        }
        _logger.Log("Task Deleted", $"Task #{taskId} deleted.");
    }
}
