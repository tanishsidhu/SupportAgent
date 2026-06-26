using SupportAgent.Tools;
using SupportAgent.Ui;

namespace SupportAgent.Tests;

public class ReviewUiTests
{
    [Fact]
    public void GetPending_ReturnsOnlyUnreviewedPendingItems()
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

            tools.StageDraft(new StagedDraft(
                "T-1002",
                "pending_review",
                "Draft two",
                ["CONF-1002"],
                DateTimeOffset.UtcNow,
                HumanDecision: "approved",
                FinalAnswer: "Draft two",
                ReviewedAt: DateTimeOffset.UtcNow));

            tools.StageDraft(new StagedDraft(
                "T-1006",
                "routed",
                "Bug report",
                [],
                DateTimeOffset.UtcNow));

            var pending = ReviewQueueBuilder.GetPending(tools);

            Assert.Single(pending);
            Assert.Equal("T-1001", pending[0].TicketId);
            Assert.Contains("Idempotency", pending[0].Subject, StringComparison.OrdinalIgnoreCase);
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
