using System.Text.Json;

namespace SupportAgent;

public static class ListData
{
    private static readonly string DataRoot = Path.Combine(
        Directory.GetCurrentDirectory(),
        "data");

    public static void Run()
    {
        Console.WriteLine("=== CapStream synthetic data ===\n");

        ListDocs();
        Console.WriteLine();
        ListResolved();
        Console.WriteLine();
        ListInbox();
    }

    private static void ListDocs()
    {
        var docsDir = Path.Combine(DataRoot, "docs");
        var files = Directory.GetFiles(docsDir, "*.md").OrderBy(f => f);

        Console.WriteLine($"Docs ({files.Count()}) — simulated Confluence pages");
        foreach (var file in files)
        {
            Console.WriteLine($"  {Path.GetFileName(file)}");
        }
    }

    private static void ListResolved()
    {
        var resolvedDir = Path.Combine(DataRoot, "resolved");
        var files = Directory.GetFiles(resolvedDir, "*.json").OrderBy(f => f);

        Console.WriteLine($"Resolved ({files.Count()}) — past closed tickets");
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var id = doc.RootElement.GetProperty("id").GetString();
            var question = doc.RootElement.GetProperty("question").GetString();
            Console.WriteLine($"  {id}  {question}");
        }
    }

    private static void ListInbox()
    {
        var inboxPath = Path.Combine(DataRoot, "inbox", "tickets.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inboxPath));

        Console.WriteLine($"Inbox ({doc.RootElement.GetArrayLength()}) — backlog queue");
        foreach (var ticket in doc.RootElement.EnumerateArray())
        {
            var id = ticket.GetProperty("id").GetString();
            var subject = ticket.GetProperty("subject").GetString();
            Console.WriteLine($"  {id}  {subject}");
        }
    }
}
