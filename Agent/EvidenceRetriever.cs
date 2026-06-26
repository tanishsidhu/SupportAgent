using OpenAI.Chat;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class EvidenceRetriever : IEvidenceRetriever
{
    private readonly DeepSeekClient _client;
    private readonly SupportTools _tools;
    private readonly TextWriter _log;
    private readonly TicketTrace? _trace;

    public EvidenceRetriever(
        DeepSeekClient client,
        SupportTools tools,
        TextWriter? log = null,
        TicketTrace? trace = null)
    {
        _client = client;
        _tools = tools;
        _log = log ?? Console.Out;
        _trace = trace;
    }

    public async Task<(EvidenceSet Evidence, double Confidence)> RetrieveAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        var query = $"{ticket.Subject}. {ticket.Body}";
        var evidence = new EvidenceSet();
        var confidence = 0.0;

        for (var round = 1; round <= MakerCheckerOptions.MaxRetrievalRounds; round++)
        {
            await _log.WriteLineAsync($"=== Retrieval round {round} ===");
            await _log.WriteLineAsync($"Query: {query}");

            evidence.AddRange(_tools.SearchDocs(query));
            evidence.AddRange(_tools.SearchResolved(query));

            foreach (var hit in evidence.All.Take(5))
            {
                await _log.WriteLineAsync($"  Retrieved: {hit.SourceId} (score {hit.Score:F2})");
            }

            var assessment = await AssessEvidenceAsync(ticket, evidence, cancellationToken);
            confidence = assessment.Score;

            await _log.WriteLineAsync(
                $"Evidence confidence: {confidence:F2} — {assessment.Reason}");
            await _log.WriteLineAsync();

            if (confidence >= MakerCheckerOptions.EvidenceThreshold
                || round == MakerCheckerOptions.MaxRetrievalRounds)
            {
                break;
            }

            query = await RefineQueryAsync(ticket, query, evidence, cancellationToken);
        }

        if (_trace is not null)
        {
            _trace.RetrievedSourceIds.Clear();
            _trace.RetrievedSourceIds.AddRange(evidence.All.Select(h => h.SourceId));
            _trace.EvidenceConfidence = confidence;
        }

        return (evidence, confidence);
    }

    private async Task<EvidenceAssessment> AssessEvidenceAsync(
        Ticket ticket,
        EvidenceSet evidence,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                Score whether the evidence is sufficient to answer the ticket (0.0-1.0).
                Reply JSON only: {"score":0.0,"reason":"one line"}
                """),
            new UserChatMessage(
                $"""
                Ticket: {ticket.Subject}
                Question: {ticket.Body}

                Evidence:
                {evidence.FormatForPrompt()}
                """),
        };

        var response = await _client.CompleteAsync(messages, cancellationToken);
        return EvidenceAssessment.Parse(response);
    }

    private async Task<string> RefineQueryAsync(
        Ticket ticket,
        string currentQuery,
        EvidenceSet evidence,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "Suggest one improved BM25 search query. Reply with the query text only, no JSON."),
            new UserChatMessage(
                $"""
                Ticket: {ticket.Subject}
                Question: {ticket.Body}
                Previous query: {currentQuery}
                Evidence found so far: {string.Join(", ", evidence.All.Select(h => h.SourceId))}
                """),
        };

        return (await _client.CompleteAsync(messages, cancellationToken)).Trim();
    }
}
