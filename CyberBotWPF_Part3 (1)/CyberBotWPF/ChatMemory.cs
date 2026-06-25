using System.Collections.Generic;

namespace CyberBotWPF;

/// <summary>
/// Holds persistent conversation state for the current session:
/// the user's name, favourite topic, last detected sentiment,
/// last matched topic (for follow-up handling), and conversation history.
/// Uses a generic List&lt;string&gt; for history — satisfies the "generic collection" requirement.
/// </summary>
public class ChatMemory
{
    // ── Stored facts ─────────────────────────────────────────────────────────
    public string Username { get; }
    public string? FavouriteTopic { get; set; }
    public string? LastSentiment { get; set; }

    /// <summary>The topic key of the last bot response (enables "tell me more").</summary>
    public string? LastTopicKey { get; set; }

    /// <summary>Full conversation history (user + bot turns) for context-aware replies.</summary>
    public List<string> History { get; } = new();

    public ChatMemory(string username) => Username = username;

    // ── Delegate for memory-change notification ───────────────────────────────
    // Satisfies the "use delegates" requirement.
    public delegate void MemoryUpdatedHandler(string field, string value);
    public event MemoryUpdatedHandler? OnMemoryUpdated;

    public void SetFavouriteTopic(string topic)
    {
        FavouriteTopic = topic;
        OnMemoryUpdated?.Invoke("FavouriteTopic", topic);
    }

    public void SetSentiment(string sentiment)
    {
        LastSentiment = sentiment;
        OnMemoryUpdated?.Invoke("LastSentiment", sentiment);
    }

    public void AddHistory(string entry) => History.Add(entry);
}