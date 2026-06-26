using System.Text.Json;
using System.Text.RegularExpressions;

namespace SupportAgent.Tools;

public sealed class SupportTools
{
    private const double ResolvedScoreBoost = 1.25;

    private readonly string _dataRoot;
    private readonly string _inboxPath;
    private readonly string _reviewQueueDir;
    private readonly string _resolvedDir;
    private readonly string _tracesDir;
    private readonly string _amendmentsLogPath;
    private readonly IRetrievalIndex _docsIndex;
    private readonly IRetrievalIndex _resolvedIndex;

    public SupportTools(string dataRoot)
    {
        _dataRoot = dataRoot;
        _inboxPath = Path.Combine(dataRoot, "inbox", "tickets.json");
        _reviewQueueDir = Path.Combine(dataRoot, "review_queue");
        _resolvedDir = Path.Combine(dataRoot, "resolved");
        _tracesDir = Path.Combine(dataRoot, "traces");
        _amendmentsLogPath = Path.Combine(dataRoot, "amendments.log");

        _docsIndex = new Bm25Index(LoadDocs(Path.Combine(dataRoot, "docs")));
        _resolvedIndex = new Bm25Index(LoadResolved(_resolvedDir));
    }

    public string TracesDir => _tracesDir;

    public Ticket GetTicket(string id)
    {
        var ticket = GetAllTickets().FirstOrDefault(t => t.Id == id);

        if (ticket is null)
        {
            throw new KeyNotFoundException($"Ticket not found: {id}");
        }

        return ticket;
    }

    public IReadOnlyList<Ticket> GetAllTickets() => LoadInboxTickets();

    public IReadOnlyList<SearchHit> SearchDocs(string query) =>
        _docsIndex.Search(query);

    public IReadOnlyList<SearchHit> SearchResolved(string query)
    {
        return _resolvedIndex
            .Search(query)
            .Select(hit => hit with { Score = hit.Score * ResolvedScoreBoost })
            .OrderByDescending(hit => hit.Score)
            .ToList();
    }

    public StagedDraft StageDraft(StagedDraft record)
    {
        TicketId.Require(record.TicketId);
        Directory.CreateDirectory(_reviewQueueDir);

        var fileName = $"{record.TicketId}-{record.StagedAt:yyyyMMddHHmmss}.json";
        var path = Path.Combine(_reviewQueueDir, fileName);

        SaveStagedDraft(path, record);
        return record;
    }

    public void SaveStagedDraft(string path, StagedDraft record)
    {
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public IReadOnlyList<StagedDraft> ListStagedDrafts()
    {
        if (!Directory.Exists(_reviewQueueDir))
        {
            return [];
        }

        return Directory.GetFiles(_reviewQueueDir, "*.json")
            .Select(path => LoadStagedDraft(path))
            .OrderBy(d => d.TicketId)
            .ToList();
    }

    public StagedDraft LoadStagedDraft(string path)
    {
        return JsonSerializer.Deserialize<StagedDraft>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Could not read staged draft: {path}");
    }

    public bool IsTicketStaged(string ticketId) => FindLatestStaged(ticketId) is not null;

    public IReadOnlySet<string> GetStagedTicketIds() =>
        ListStagedDrafts().Select(d => d.TicketId).ToHashSet(StringComparer.Ordinal);

    public (string Path, StagedDraft Draft)? FindLatestStaged(string ticketId)
    {
        TicketId.Require(ticketId);

        if (!Directory.Exists(_reviewQueueDir))
        {
            return null;
        }

        var file = Directory.GetFiles(_reviewQueueDir, $"{ticketId}-*.json")
            .OrderByDescending(f => f, StringComparer.Ordinal)
            .FirstOrDefault();

        return file is null ? null : (file, LoadStagedDraft(file));
    }

    public string AppendResolved(Ticket ticket, string acceptedAnswer, IReadOnlyList<string> linkedDocs)
    {
        Directory.CreateDirectory(_resolvedDir);

        var nextId = NextResolvedId();
        var record = new
        {
            id = nextId,
            question = $"{ticket.Subject}\n{ticket.Body}",
            accepted_answer = acceptedAnswer,
            linked_docs = linkedDocs,
        };

        var path = Path.Combine(_resolvedDir, $"{nextId}.json");
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));

        return nextId;
    }

    public void LogAmendment(string ticketId, string originalDraft, string approvedAnswer)
    {
        Directory.CreateDirectory(_dataRoot);
        var line =
            $"[{DateTimeOffset.UtcNow:u}] {ticketId}\n" +
            $"  ORIGINAL: {originalDraft}\n" +
            $"  APPROVED: {approvedAnswer}\n";

        File.AppendAllText(_amendmentsLogPath, line);
    }

    private string NextResolvedId()
    {
        if (!Directory.Exists(_resolvedDir))
        {
            return "RES-2001";
        }

        var max = Directory.GetFiles(_resolvedDir, "RES-*.json")
            .Select(path => int.Parse(Path.GetFileNameWithoutExtension(path).Split('-')[1]))
            .DefaultIfEmpty(2000)
            .Max();

        return $"RES-{max + 1}";
    }

    private List<Ticket> LoadInboxTickets()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(_inboxPath));

        return doc.RootElement.EnumerateArray()
            .Select(element => new Ticket(
                element.GetProperty("id").GetString()!,
                element.GetProperty("subject").GetString()!,
                element.GetProperty("body").GetString()!))
            .ToList();
    }

    private static IEnumerable<(string Id, string Text)> LoadDocs(string docsDir)
    {
        foreach (var file in Directory.GetFiles(docsDir, "*.md"))
        {
            var content = File.ReadAllText(file);
            var id = ExtractFrontMatterValue(content, "id")
                ?? throw new InvalidDataException($"Doc missing id in front matter: {file}");
            var body = StripFrontMatter(content).Trim();
            yield return (id, body);
        }
    }

    private static IEnumerable<(string Id, string Text)> LoadResolved(string resolvedDir)
    {
        if (!Directory.Exists(resolvedDir))
        {
            yield break;
        }

        foreach (var file in Directory.GetFiles(resolvedDir, "*.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetString()!;
            var question = root.GetProperty("question").GetString()!;
            var answer = root.GetProperty("accepted_answer").GetString()!;
            yield return (id, $"{question}\n{answer}");
        }
    }

    private static string? ExtractFrontMatterValue(string markdown, string key)
    {
        var match = Regex.Match(
            markdown,
            $@"^---\s*\n(?:.*\n)*?^{key}:\s*(.+)$",
            RegexOptions.Multiline);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string StripFrontMatter(string markdown)
    {
        return Regex.Replace(markdown, @"^---\s*\n.*?\n---\s*\n", "", RegexOptions.Singleline);
    }
}
