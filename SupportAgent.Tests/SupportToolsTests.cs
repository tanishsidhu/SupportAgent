using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class SupportToolsTests
{
    private static string ProjectDataRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));

    private static SupportTools CreateTools(string? dataRoot = null) =>
        new(dataRoot ?? ProjectDataRoot);

    [Fact]
    public void GetTicket_ReturnsMatchingTicket()
    {
        var tools = CreateTools();
        var ticket = tools.GetTicket("T-1001");

        Assert.Equal("T-1001", ticket.Id);
        Assert.Contains("idempotent", ticket.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTicket_ThrowsWhenMissing()
    {
        var tools = CreateTools();
        Assert.Throws<KeyNotFoundException>(() => tools.GetTicket("T-9999"));
    }

    [Fact]
    public void SearchDocs_FindsIdempotencyDoc()
    {
        var tools = CreateTools();
        var hits = tools.SearchDocs("idempotency key header");

        Assert.NotEmpty(hits);
        Assert.Equal("CONF-1009", hits[0].SourceId);
        Assert.Contains("Idempotency-Key", hits[0].Passage);
    }

    [Fact]
    public void SearchDocs_FindsRateLimitDoc()
    {
        var tools = CreateTools();
        var hits = tools.SearchDocs("production requests per minute");

        Assert.NotEmpty(hits);
        Assert.Contains(hits, hit => hit.SourceId == "CONF-1002");
    }

    [Fact]
    public void SearchResolved_FindsDuplicateWebhookAnswer()
    {
        var tools = CreateTools();
        var hits = tools.SearchResolved("duplicate webhook lease approved");

        Assert.NotEmpty(hits);
        Assert.Equal("RES-2003", hits[0].SourceId);
        Assert.Contains("at-least-once", hits[0].Passage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchResolved_BoostsScoreAboveMatchingDoc()
    {
        var tools = CreateTools();
        var query = "webhook delivery events";

        var docHits = tools.SearchDocs(query);
        var resolvedHits = tools.SearchResolved(query);

        Assert.NotEmpty(docHits);
        Assert.NotEmpty(resolvedHits);

        var topDocScore = docHits[0].Score;
        var topResolvedScore = resolvedHits[0].Score;

        Assert.True(
            topResolvedScore > topDocScore,
            "Resolved answers should rank higher than docs for overlapping topics.");
    }

    [Fact]
    public void StageDraft_WritesPendingReviewRecord()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempRoot, "inbox"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "resolved"));
        File.Copy(
            Path.Combine(ProjectDataRoot, "inbox", "tickets.json"),
            Path.Combine(tempRoot, "inbox", "tickets.json"));

        try
        {
            var tools = CreateTools(tempRoot);
            var stagedAt = DateTimeOffset.UtcNow;
            var draft = new StagedDraft(
                "T-1001",
                "pending_review",
                "Use the Idempotency-Key header.",
                ["CONF-1009"],
                stagedAt);

            tools.StageDraft(draft);

            var reviewDir = Path.Combine(tempRoot, "review_queue");
            var file = Directory.GetFiles(reviewDir, "T-1001-*.json").Single();

            Assert.Contains("pending_review", File.ReadAllText(file));
            Assert.Contains("CONF-1009", File.ReadAllText(file));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
