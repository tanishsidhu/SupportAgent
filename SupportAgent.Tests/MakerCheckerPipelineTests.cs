using SupportAgent.Agent;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class MakerCheckerPipelineTests
{
    private sealed class SequenceChecker : IDraftChecker
    {
        private int _calls;

        public Task<CheckerVerdict> VerifyAsync(
            Ticket ticket,
            DraftResult draft,
            EvidenceSet evidence,
            CancellationToken cancellationToken = default)
        {
            _calls++;

            if (_calls == 1)
            {
                return Task.FromResult(new CheckerVerdict(
                    false,
                    ["Claim not grounded in cited source."],
                    "amend"));
            }

            return Task.FromResult(new CheckerVerdict(true, [], "post"));
        }
    }

    private sealed class FixedMaker : IDraftMaker
    {
        public Task<DraftResult> DraftAsync(
            Ticket ticket,
            EvidenceSet evidence,
            IReadOnlyList<string>? revisionNotes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DraftResult(
                revisionNotes is { Count: > 0 }
                    ? "Revised grounded answer."
                    : "First draft with issue.",
                ["CONF-1009"],
                0.8));
    }

    private sealed class FixedRetriever : IEvidenceRetriever
    {
        public Task<(EvidenceSet Evidence, double Confidence)> RetrieveAsync(
            Ticket ticket,
            CancellationToken cancellationToken = default)
        {
            var evidence = new EvidenceSet();
            evidence.AddRange([new SearchHit("CONF-1009", "Idempotency-Key header.", 2.0)]);
            return Task.FromResult((evidence, 0.9));
        }
    }

    [Fact]
    public async Task Pipeline_CheckerRejectThenPass_StagesPendingReview()
    {
        var tempRoot = CreateTempDataRoot();
        var log = new StringWriter();

        try
        {
            var tools = new SupportTools(tempRoot);
            var client = new DeepSeekClient(new DeepSeekSettings("test-key"));
            var pipeline = new MakerCheckerPipeline(
                client,
                tools,
                log,
                new FixedMaker(),
                new SequenceChecker(),
                new FixedRetriever());

            await pipeline.RunAsync(tools.GetTicket("T-1001"), "T-1001");

            var staged = File.ReadAllText(
                Directory.GetFiles(Path.Combine(tempRoot, "review_queue"), "T-1001-*.json").Single());

            Assert.Contains("pending_review", staged);
            Assert.Contains("Revised grounded answer.", staged);
            Assert.Contains("Maker attempt 2", log.ToString());
            Assert.Contains("Pass: False", log.ToString());
            Assert.Contains("Pass: True", log.ToString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Pipeline_LowEvidenceConfidence_Escalates()
    {
        var tempRoot = CreateTempDataRoot();

        try
        {
            var tools = new SupportTools(tempRoot);
            var client = new DeepSeekClient(new DeepSeekSettings("test-key"));
            var pipeline = new MakerCheckerPipeline(
                client,
                tools,
                retriever: new LowConfidenceRetriever());

            await pipeline.RunAsync(tools.GetTicket("T-1001"), "T-1001");

            var staged = File.ReadAllText(
                Directory.GetFiles(Path.Combine(tempRoot, "review_queue"), "T-1001-*.json").Single());

            Assert.Contains("escalated", staged);
            Assert.Contains("Insufficient evidence", staged);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private sealed class LowConfidenceRetriever : IEvidenceRetriever
    {
        public Task<(EvidenceSet Evidence, double Confidence)> RetrieveAsync(
            Ticket ticket,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((new EvidenceSet(), 0.2));
    }

    private static string CreateTempDataRoot()
    {
        var projectData = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));

        var tempRoot = Path.Combine(Path.GetTempPath(), "supportagent-mc-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempRoot, "inbox"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "resolved"));

        File.Copy(
            Path.Combine(projectData, "inbox", "tickets.json"),
            Path.Combine(tempRoot, "inbox", "tickets.json"));

        return tempRoot;
    }
}
