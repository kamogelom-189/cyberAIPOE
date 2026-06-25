using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberBotWPF.Managers;

/// <summary>
/// Pure C# "NLP simulation". No AI / external APIs are used.
/// Normalizes user input (lowercase, punctuation stripped) and matches it
/// against synonym dictionaries using Contains / StartsWith / EndsWith /
/// Regex, so many different phrasings resolve to the same Intent.
/// </summary>
public enum Intent
{
    None,
    AddTask,
    ShowTasks,
    DeleteTask,
    CompleteTask,
    SetReminder,
    StartQuiz,
    ShowActivityLog,
    TopicPassword,
    TopicPhishing,
    TopicMalware,
    TopicSocialEngineering,
    TopicSafeBrowsing,
    TopicPublicWifi,
    TopicEncryption,
    TopicFirewall,
    TopicUpdates,
    TopicRansomware,
    TopicIdentityTheft,
    TopicOnlineShopping,
    TopicEmailSafety,
    Topic2FA,
    TopicVpn,
    TopicBackup
}

public class NLPProcessor
{
    // Each intent maps to a list of trigger phrases / fragments.
    // Contains() is used for free-form matching; Regex handles flexible
    // patterns like "remind me in 7 days" or "set a reminder for tomorrow".
    private static readonly Dictionary<Intent, string[]> _intentPhrases = new()
    {
        [Intent.AddTask] = new[]
        {
            "add task", "add a task", "create task", "create a task", "new task",
            "log a task", "make a task", "add to do", "add todo"
        },
        [Intent.ShowTasks] = new[]
        {
            "show my tasks", "show tasks", "view tasks", "view my tasks", "list tasks",
            "see my tasks", "what are my tasks", "my to do list", "show reminders", "view reminders"
        },
        [Intent.DeleteTask] = new[]
        {
            "delete task", "remove task", "delete a task", "remove a task", "cancel task"
        },
        [Intent.CompleteTask] = new[]
        {
            "complete task", "mark task done", "mark task complete", "mark as done",
            "finish task", "task done", "i finished", "i completed"
        },
        [Intent.SetReminder] = new[]
        {
            "remind me", "set a reminder", "set reminder", "new reminder", "reminder for", "schedule a reminder"
        },
        [Intent.StartQuiz] = new[]
        {
            "quiz", "start quiz", "play quiz", "take the quiz", "game", "test my knowledge"
        },
        [Intent.ShowActivityLog] = new[]
        {
            "activity log", "show activity log", "history", "show history", "recent activity",
            "what have you done", "what did you do", "show logs", "recent actions", "show log"
        },
        [Intent.TopicPassword] = new[]
        {
            "password help", "password advice", "password tips", "tell me about passwords", "passphrase"
        },
        [Intent.TopicPhishing] = new[]
        {
            "what is phishing", "explain phishing", "email scam", "fake email", "phishing"
        },
        [Intent.TopicMalware] = new[] { "virus", "malware", "trojan", "ransomware", "spyware" },
        [Intent.TopicSocialEngineering] = new[] { "social engineering", "pretexting", "manipulation tactics" },
        [Intent.TopicSafeBrowsing] = new[] { "safe browsing", "browsing safely", "browser security" },
        [Intent.TopicPublicWifi] = new[] { "public wifi", "public wi-fi", "open wifi", "coffee shop wifi" },
        [Intent.TopicEncryption] = new[] { "encryption", "encrypt my data", "encrypted" },
        [Intent.TopicFirewall] = new[] { "firewall", "network security" },
        [Intent.TopicUpdates] = new[] { "software update", "updates", "patching", "patch my system" },
        [Intent.TopicRansomware] = new[] { "ransomware" },
        [Intent.TopicIdentityTheft] = new[] { "identity theft", "stolen identity", "someone stole my identity" },
        [Intent.TopicOnlineShopping] = new[] { "online shopping", "shopping online", "safe shopping" },
        [Intent.TopicEmailSafety] = new[] { "email safety", "safe email", "email security" },
        [Intent.Topic2FA] = new[] { "2fa", "two factor", "two-factor", "mfa", "multi factor", "enable mfa", "authenticator" },
        [Intent.TopicVpn] = new[] { "vpn", "virtual private network" },
        [Intent.TopicBackup] = new[] { "backup", "back up", "restore my files" },
    };

    /// <summary>Normalizes raw input: lowercase + strip punctuation + collapse whitespace.</summary>
    public static string Normalize(string input)
    {
        string lower = input.Trim().ToLowerInvariant();
        string noPunctuation = Regex.Replace(lower, @"[^\w\s]", " ");
        return Regex.Replace(noPunctuation, @"\s+", " ").Trim();
    }

    /// <summary>Returns the best-matching intent for the given (already normalized) input.</summary>
    public Intent DetectIntent(string normalizedInput)
    {
        // Regex-based flexible reminder detection ("remind me in 7 days", "reminder tomorrow", etc.)
        if (Regex.IsMatch(normalizedInput, @"remind(er)?\s.*\b(today|tomorrow|\d+\s*day)"))
            return Intent.SetReminder;

        foreach (var (intent, phrases) in _intentPhrases)
        {
            foreach (var phrase in phrases)
            {
                if (normalizedInput.Contains(phrase) ||
                    normalizedInput.StartsWith(phrase) ||
                    normalizedInput.EndsWith(phrase))
                {
                    return intent;
                }
            }
        }

        return Intent.None;
    }

    /// <summary>Extracts a likely task title from phrases like "add a task to update my password".</summary>
    public static string ExtractTaskTitle(string rawInput)
    {
        string norm = rawInput.Trim();
        string[] markers = { "add a task to ", "add task to ", "create a task to ", "create task to ",
                              "new task to ", "add a task ", "add task ", "create task ", "new task " };
        foreach (var marker in markers)
        {
            int idx = norm.ToLower().IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                string title = norm.Substring(idx + marker.Length).Trim();
                if (title.Length > 0)
                    return char.ToUpper(title[0]) + title.Substring(1);
            }
        }
        return "New Cybersecurity Task";
    }
}
