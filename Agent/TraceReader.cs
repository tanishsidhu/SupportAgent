using System.Text.Json;

namespace SupportAgent.Agent;

public static class TraceReader
{
    public static IReadOnlyList<TicketTrace> LoadAll(string tracesDir)
    {
        if (!Directory.Exists(tracesDir))
        {
            return [];
        }

        return Directory.GetFiles(tracesDir, "*.json")
            .Select(Load)
            .OrderByDescending(trace => trace.CompletedAt)
            .ToList();
    }

    public static TicketTrace? LoadLatest(string tracesDir, string ticketId)
    {
        if (!Directory.Exists(tracesDir))
        {
            return null;
        }

        return Directory.GetFiles(tracesDir, $"{ticketId}-*.json")
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .Select(Load)
            .FirstOrDefault();
    }

    public static TicketTrace Load(string path)
    {
        return JsonSerializer.Deserialize<TicketTrace>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Could not read trace: {path}");
    }
}
