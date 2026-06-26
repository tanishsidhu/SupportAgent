using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class MakerCheckerPipeline
{
    private readonly IEvidenceRetriever _retriever;
    private readonly IDraftMaker _maker;
    private readonly IDraftChecker _checker;
    private readonly SupportTools _tools;
    private readonly TicketTrace? _trace;
    private readonly TextWriter _log;

    public MakerCheckerPipeline(
        DeepSeekClient client,
        SupportTools tools,
        TextWriter? log = null,
        IDraftMaker? maker = null,
        IDraftChecker? checker = null,
        IEvidenceRetriever? retriever = null,
        TicketTrace? trace = null)
    {
        _log = log ?? Console.Out;
        _tools = tools;
        _trace = trace;
        _retriever = retriever ?? new EvidenceRetriever(client, tools, _log, trace);
        _maker = maker ?? new DraftMaker(client);
        _checker = checker ?? new DraftChecker(client);
    }

    public async Task<string> RunAsync(
        Ticket ticket,
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        var (evidence, evidenceConfidence) =
            await _retriever.RetrieveAsync(ticket, cancellationToken);

        if (evidenceConfidence < MakerCheckerOptions.EvidenceThreshold)
        {
            return StageEscalation(
                ticketId,
                $"Insufficient evidence after retrieval (confidence {evidenceConfidence:F2}).",
                evidenceConfidence,
                null,
                "escalate");
        }

        IReadOnlyList<string>? revisionNotes = null;

        for (var attempt = 1; attempt <= MakerCheckerOptions.MaxRevisionAttempts; attempt++)
        {
            await _log.WriteLineAsync($"=== Maker attempt {attempt} ===");

            var draft = await _maker.DraftAsync(ticket, evidence, revisionNotes, cancellationToken);
            RecordDraft(draft);

            await _log.WriteLineAsync($"Draft (confidence {draft.AnswerConfidence:F2}):");
            await _log.WriteLineAsync(draft.DraftAnswer);
            await _log.WriteLineAsync($"Citations: {string.Join(", ", draft.Citations)}");
            await _log.WriteLineAsync();

            await _log.WriteLineAsync("=== Checker ===");
            var verdict = await _checker.VerifyAsync(ticket, draft, evidence, cancellationToken);
            RecordChecker(verdict);

            await _log.WriteLineAsync($"Pass: {verdict.Pass}");
            await _log.WriteLineAsync($"Suggested action: {verdict.SuggestedAction}");

            if (verdict.Problems.Count > 0)
            {
                await _log.WriteLineAsync("Problems:");
                foreach (var problem in verdict.Problems)
                {
                    await _log.WriteLineAsync($"  - {problem}");
                }
            }

            await _log.WriteLineAsync();

            if (verdict.Pass)
            {
                StagePendingReview(ticketId, draft, evidenceConfidence, verdict);
                await _log.WriteLineAsync("Staged → pending_review");
                return draft.DraftAnswer;
            }

            revisionNotes = verdict.Problems;
        }

        return StageEscalation(
            ticketId,
            "Checker could not approve draft after revision cap.",
            evidenceConfidence,
            revisionNotes,
            "escalate");
    }

    private void RecordDraft(DraftResult draft)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.DraftAnswer = draft.DraftAnswer;
        _trace.Citations.Clear();
        _trace.Citations.AddRange(draft.Citations);
        _trace.AnswerConfidence = draft.AnswerConfidence;
    }

    private void RecordChecker(CheckerVerdict verdict)
    {
        if (_trace is null)
        {
            return;
        }

        _trace.CheckerPass = verdict.Pass;
        _trace.CheckerProblems.Clear();
        _trace.CheckerProblems.AddRange(verdict.Problems);
        _trace.SuggestedAction = verdict.SuggestedAction;
    }

    private void StagePendingReview(
        string ticketId,
        DraftResult draft,
        double evidenceConfidence,
        CheckerVerdict verdict)
    {
        if (_trace is not null)
        {
            _trace.StagedStatus = "pending_review";
        }

        _tools.StageDraft(new StagedDraft(
            ticketId,
            "pending_review",
            draft.DraftAnswer,
            draft.Citations,
            DateTimeOffset.UtcNow,
            evidenceConfidence,
            draft.AnswerConfidence,
            verdict.Pass,
            verdict.Problems,
            verdict.SuggestedAction));
    }

    private string StageEscalation(
        string ticketId,
        string reason,
        double evidenceConfidence,
        IReadOnlyList<string>? problems,
        string suggestedAction)
    {
        if (_trace is not null)
        {
            _trace.StagedStatus = "escalated";
        }

        _tools.StageDraft(new StagedDraft(
            ticketId,
            "escalated",
            reason,
            [],
            DateTimeOffset.UtcNow,
            evidenceConfidence,
            null,
            false,
            problems,
            suggestedAction));

        _log.WriteLine($"Short-circuited → staged as escalated: {reason}");
        return reason;
    }
}
