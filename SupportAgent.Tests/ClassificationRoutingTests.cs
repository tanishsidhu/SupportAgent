using SupportAgent.Agent;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class ClassificationRoutingTests
{
    private sealed class FixedClassifier(TicketClassification result) : ITicketClassifier
    {
        public Task<TicketClassification> ClassifyAsync(
            Ticket ticket,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    [Theory]
    [InlineData(TicketBucket.NotAQuestion, "routed")]
    [InlineData(TicketBucket.NeedsHuman, "escalated")]
    public async Task RunAsync_ShortCircuitsNonAnswerable(TicketBucket bucket, string expectedStatus)
    {
        var tempRoot = CreateTempDataRoot();
        try
        {
            var tools = new SupportTools(tempRoot);
            var client = new DeepSeekClient(new DeepSeekSettings("test-key"));
            var classification = new TicketClassification(bucket, "Short-circuit reason.");
            var loop = new AgentLoop(client, tools, classifier: new FixedClassifier(classification));

            await loop.RunAsync("T-1001");

            var staged = Directory.GetFiles(Path.Combine(tempRoot, "review_queue"), "T-1001-*.json").Single();
            var json = File.ReadAllText(staged);

            Assert.Contains(expectedStatus, json);
            Assert.Contains("Short-circuit reason.", json);
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

        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-classify-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempRoot, "inbox"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "resolved"));

        File.Copy(
            Path.Combine(projectData, "inbox", "tickets.json"),
            Path.Combine(tempRoot, "inbox", "tickets.json"));

        return tempRoot;
    }
}
