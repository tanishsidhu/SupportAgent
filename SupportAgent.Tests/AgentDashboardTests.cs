using SupportAgent.Agent;
using SupportAgent.Tools;
using SupportAgent.Ui;

namespace SupportAgent.Tests;

public class AgentDashboardTests
{
    [Fact]
    public void DraftEnricher_FillsMissingFieldsFromTrace()
    {
        var draft = new StagedDraft(
            "T-1001",
            "pending_review",
            "Draft",
            ["CONF-1001"],
            DateTimeOffset.UtcNow);

        var trace = new TicketTrace { TicketId = "T-1001" };
        trace.EvidenceConfidence = 0.88;
        trace.AnswerConfidence = 0.91;
        trace.CheckerPass = true;
        trace.SuggestedAction = "post";

        var enriched = DraftEnricher.Enrich(draft, trace);

        Assert.Equal(0.88, enriched.EvidenceConfidence);
        Assert.Equal(0.91, enriched.AnswerConfidence);
        Assert.True(enriched.CheckerPass);
        Assert.Equal("post", enriched.SuggestedAction);
    }

    [Fact]
    public void GetPending_EnrichesDraftFromTraceWhenStagedFileLacksMetadata()
    {
        var tempRoot = CreateTempDataRoot();

        try
        {
            var tools = new SupportTools(tempRoot);
            tools.StageDraft(new StagedDraft(
                "T-1001",
                "pending_review",
                "Draft one",
                ["CONF-1009"],
                DateTimeOffset.UtcNow));

            var trace = new TicketTrace
            {
                TicketId = "T-1001",
                CompletedAt = DateTimeOffset.UtcNow,
            };
            trace.EvidenceConfidence = 0.95;
            trace.AnswerConfidence = 0.87;
            trace.CheckerPass = true;
            trace.SuggestedAction = "post";
            trace.ClassificationBucket = "Answerable";
            trace.ClassificationReason = "Doc question";
            TraceWriter.Save(trace, tools.TracesDir);

            var pending = ReviewQueueBuilder.GetPending(tools);

            Assert.Single(pending);
            Assert.Equal(0.95, pending[0].Draft.EvidenceConfidence);
            Assert.Equal(0.87, pending[0].Draft.AnswerConfidence);
            Assert.True(pending[0].Draft.CheckerPass);
            Assert.NotEmpty(pending[0].Pipeline);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Build_ReportsInboxPhases()
    {
        var tempRoot = CreateTempDataRoot();

        try
        {
            var tools = new SupportTools(tempRoot);
            tools.StageDraft(new StagedDraft(
                "T-1001",
                "pending_review",
                "Draft",
                [],
                DateTimeOffset.UtcNow));

            tools.StageDraft(new StagedDraft(
                "T-1006",
                "routed",
                "Bug",
                [],
                DateTimeOffset.UtcNow));

            var snapshot = AgentDashboard.Build(tools, Path.Combine(tempRoot, "LOOP_STATE.json"));

            Assert.Equal(10, snapshot.InboxTotal);
            Assert.Equal(1, snapshot.InReview);
            Assert.Equal(1, snapshot.Routed);
            Assert.True(snapshot.PendingAgent >= 8);
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

        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-ui-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempRoot, "inbox"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "resolved"));

        File.Copy(
            Path.Combine(projectData, "inbox", "tickets.json"),
            Path.Combine(tempRoot, "inbox", "tickets.json"));

        return tempRoot;
    }
}
