using System.Text.Json;

namespace SupportAgent.Agent;

public sealed record LoopStateEntry(string TicketId, DateTimeOffset CompletedAt, int ApiCallsUsed);

public sealed class LoopState
{
    public DateTimeOffset? LastRunAt { get; set; }
    public List<LoopStateEntry> Completed { get; set; } = [];

    public bool IsCompleted(string ticketId) =>
        Completed.Any(entry => entry.TicketId == ticketId);

    public void MarkCompleted(string ticketId, int apiCallsUsed)
    {
        Completed.RemoveAll(entry => entry.TicketId == ticketId);
        Completed.Add(new LoopStateEntry(ticketId, DateTimeOffset.UtcNow, apiCallsUsed));
        LastRunAt = DateTimeOffset.UtcNow;
    }

    public static LoopState Load(string path)
    {
        if (!File.Exists(path))
        {
            return new LoopState();
        }

        return JsonSerializer.Deserialize<LoopState>(File.ReadAllText(path)) ?? new LoopState();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
