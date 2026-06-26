using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class AgentLoop : IAgentRunner
{
    private readonly DeepSeekClient _client;
    private readonly SupportTools _tools;
    private readonly ITicketClassifier _classifier;
    private readonly int _maxTurns;
    private readonly bool _demoForbiddenPost;
    private readonly TextWriter _log;

    public AgentLoop(
        DeepSeekClient client,
        SupportTools tools,
        int maxTurns = 10,
        bool demoForbiddenPost = false,
        ITicketClassifier? classifier = null,
        TextWriter? log = null)
    {
        _client = client;
        _tools = tools;
        _classifier = classifier ?? new TicketClassifier(client);
        _maxTurns = maxTurns;
        _demoForbiddenPost = demoForbiddenPost;
        _log = log ?? Console.Out;
    }

    public async Task<string> RunAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = _tools.GetTicket(ticketId);
        var trace = new TicketTrace { TicketId = ticketId };

        await _log.WriteLineAsync("=== Classification ===");
        var classification = await _classifier.ClassifyAsync(ticket, cancellationToken);
        trace.ClassificationBucket = classification.Bucket.ToString();
        trace.ClassificationReason = classification.Reason;

        await _log.WriteLineAsync($"Bucket: {classification.Bucket}");
        await _log.WriteLineAsync($"Reason: {classification.Reason}");
        await _log.WriteLineAsync();

        string result;

        if (classification.Bucket != TicketBucket.Answerable)
        {
            var staged = new StagedDraft(
                ticketId,
                classification.StageStatus,
                classification.Reason,
                [],
                DateTimeOffset.UtcNow);

            _tools.StageDraft(staged);
            trace.StagedStatus = classification.StageStatus;
            await _log.WriteLineAsync($"Short-circuited → staged as {classification.StageStatus}");
            result = classification.Reason;
        }
        else
        {
            if (_demoForbiddenPost)
            {
                await _log.WriteLineAsync(
                    "Note: --demo-forbidden-post applies to the legacy tool loop only.\n");
            }

            var pipeline = new MakerCheckerPipeline(
                _client,
                _tools,
                _log,
                trace: trace);

            result = await pipeline.RunAsync(ticket, ticketId, cancellationToken);
        }

        trace.CompletedAt = DateTimeOffset.UtcNow;
        var tracePath = TraceWriter.Save(trace, _tools.TracesDir);
        await _log.WriteLineAsync($"Trace saved: {tracePath}");
        TraceWriter.PrintSummary(trace, _log);

        return result;
    }
}
