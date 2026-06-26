using SupportAgent.Agent;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class InboxSchedulerTests
{
    private sealed class FakeAgentRunner(ApiCallTracker tracker) : IAgentRunner
    {
        public List<string> ProcessedIds { get; } = [];
        public int CallsPerTicket { get; init; } = 1;

        public async Task<string> RunAsync(string ticketId, CancellationToken cancellationToken = default)
        {
            ProcessedIds.Add(ticketId);

            for (var i = 0; i < CallsPerTicket; i++)
            {
                tracker.RecordCall();
            }

            await Task.CompletedTask;
            return "ok";
        }
    }

    [Fact]
    public async Task RunAsync_SkipsAlreadyStagedTickets()
    {
        var tempRoot = CreateTempDataRoot();
        var statePath = Path.Combine(tempRoot, "LOOP_STATE.json");

        try
        {
            var tools = new SupportTools(tempRoot);
            tools.StageDraft(new StagedDraft(
                "T-1001",
                "routed",
                "already done",
                [],
                DateTimeOffset.UtcNow));

            var tracker = new ApiCallTracker();
            var client = new DeepSeekClient(new DeepSeekSettings("test-key"), tracker);
            var runner = new FakeAgentRunner(tracker);
            var scheduler = new InboxScheduler(runner, tools, client, statePath, maxTicketsPerRun: 10, maxApiCalls: 100);

            var result = await scheduler.RunAsync();

            Assert.Equal(1, result.Skipped);
            Assert.Equal(9, result.Processed);
            Assert.DoesNotContain("T-1001", runner.ProcessedIds);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_RespectsTicketCap()
    {
        var tempRoot = CreateTempDataRoot();
        var statePath = Path.Combine(tempRoot, "LOOP_STATE.json");

        try
        {
            var tools = new SupportTools(tempRoot);
            var tracker = new ApiCallTracker();
            var client = new DeepSeekClient(new DeepSeekSettings("test-key"), tracker);
            var runner = new FakeAgentRunner(tracker);
            var scheduler = new InboxScheduler(runner, tools, client, statePath, maxTicketsPerRun: 2, maxApiCalls: 100);

            var result = await scheduler.RunAsync();

            Assert.Equal(2, result.Processed);
            Assert.Equal(8, result.StoppedByTicketCap);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_RespectsApiCallCap()
    {
        var tempRoot = CreateTempDataRoot();
        var statePath = Path.Combine(tempRoot, "LOOP_STATE.json");

        try
        {
            var tools = new SupportTools(tempRoot);
            var tracker = new ApiCallTracker();
            var client = new DeepSeekClient(new DeepSeekSettings("test-key"), tracker);
            var runner = new FakeAgentRunner(tracker) { CallsPerTicket = 3 };
            var scheduler = new InboxScheduler(runner, tools, client, statePath, maxTicketsPerRun: 10, maxApiCalls: 3);

            var result = await scheduler.RunAsync();

            Assert.Equal(1, result.Processed);
            Assert.True(result.StoppedByApiCap > 0);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void LoopState_SavesAndSkipsOnRerun()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-state-" + Guid.NewGuid());
        var statePath = Path.Combine(tempRoot, "LOOP_STATE.json");

        try
        {
            var state = new LoopState();
            state.MarkCompleted("T-1001", 5);
            state.Save(statePath);

            var loaded = LoopState.Load(statePath);
            Assert.True(loaded.IsCompleted("T-1001"));
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

        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-sched-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempRoot, "inbox"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "resolved"));

        File.Copy(
            Path.Combine(projectData, "inbox", "tickets.json"),
            Path.Combine(tempRoot, "inbox", "tickets.json"));

        return tempRoot;
    }
}
