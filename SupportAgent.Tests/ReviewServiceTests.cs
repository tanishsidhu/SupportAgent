using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class ReviewServiceTests
{
    [Fact]
    public void Approve_AppendsToResolvedCorpus()
    {
        var tempRoot = CreateTempDataRoot();
        try
        {
            var tools = new SupportTools(tempRoot);
            var draft = new StagedDraft(
                "T-1001",
                "pending_review",
                "Use Idempotency-Key header.",
                ["CONF-1009"],
                DateTimeOffset.UtcNow);

            tools.StageDraft(draft);

            var review = new ReviewService(tools);
            review.Approve("T-1001");

            var resolvedFiles = Directory.GetFiles(Path.Combine(tempRoot, "resolved"), "RES-*.json");
            Assert.NotEmpty(resolvedFiles);

            var resolvedJson = File.ReadAllText(resolvedFiles.OrderBy(f => f).Last());
            Assert.Contains("Idempotency-Key", resolvedJson);

            var updated = tools.FindLatestStaged("T-1001")!.Value.Draft;
            Assert.Equal("approved", updated.HumanDecision);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Amend_LogsAmendmentDistinctly()
    {
        var tempRoot = CreateTempDataRoot();
        try
        {
            var tools = new SupportTools(tempRoot);
            tools.StageDraft(new StagedDraft(
                "T-1001",
                "pending_review",
                "Original draft text.",
                ["CONF-1009"],
                DateTimeOffset.UtcNow));

            var review = new ReviewService(tools);
            review.Amend("T-1001", "Human-edited final answer.");

            var log = File.ReadAllText(Path.Combine(tempRoot, "amendments.log"));
            Assert.Contains("ORIGINAL: Original draft text.", log);
            Assert.Contains("APPROVED: Human-edited final answer.", log);

            var updated = tools.FindLatestStaged("T-1001")!.Value.Draft;
            Assert.Equal("amended", updated.HumanDecision);
            Assert.Equal("Original draft text.", updated.OriginalDraftAnswer);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void TraceWriter_SavesStructuredTrace()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-trace-" + Guid.NewGuid());
        var tracesDir = Path.Combine(tempRoot, "traces");

        try
        {
            var trace = new TicketTrace
            {
                TicketId = "T-1001",
                ClassificationBucket = "Answerable",
                ClassificationReason = "API question",
                StagedStatus = "pending_review",
            };
            trace.RetrievedSourceIds.Add("CONF-1009");
            trace.EvidenceConfidence = 0.9;

            var path = TraceWriter.Save(trace, tracesDir);
            var json = File.ReadAllText(path);

            Assert.Contains("CONF-1009", json);
            Assert.Contains("pending_review", json);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string CreateTempDataRoot()
    {
        var projectData = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));

        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-review-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempRoot, "inbox"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "resolved"));

        File.Copy(
            Path.Combine(projectData, "inbox", "tickets.json"),
            Path.Combine(tempRoot, "inbox", "tickets.json"));

        return tempRoot;
    }
}
