using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class ReviewService
{
    private readonly SupportTools _tools;

    public ReviewService(SupportTools tools)
    {
        _tools = tools;
    }

    public StagedDraft Approve(string ticketId)
    {
        var (path, draft) = LoadPending(ticketId);
        var updated = draft with
        {
            HumanDecision = "approved",
            FinalAnswer = draft.DraftAnswer,
            ReviewedAt = DateTimeOffset.UtcNow,
        };

        _tools.SaveStagedDraft(path, updated);
        _tools.AppendResolved(
            _tools.GetTicket(ticketId),
            updated.FinalAnswer!,
            updated.Citations);

        return updated;
    }

    public StagedDraft Amend(string ticketId, string amendedAnswer)
    {
        var (path, draft) = LoadPending(ticketId);
        var updated = draft with
        {
            HumanDecision = "amended",
            OriginalDraftAnswer = draft.DraftAnswer,
            FinalAnswer = amendedAnswer,
            ReviewedAt = DateTimeOffset.UtcNow,
        };

        _tools.SaveStagedDraft(path, updated);
        _tools.LogAmendment(ticketId, draft.DraftAnswer, amendedAnswer);
        _tools.AppendResolved(
            _tools.GetTicket(ticketId),
            amendedAnswer,
            updated.Citations);

        return updated;
    }

    public StagedDraft Reject(string ticketId)
    {
        var (path, draft) = LoadPending(ticketId);
        var updated = draft with
        {
            HumanDecision = "rejected",
            Status = "escalated",
            ReviewedAt = DateTimeOffset.UtcNow,
        };

        _tools.SaveStagedDraft(path, updated);
        return updated;
    }

    private (string Path, StagedDraft Draft) LoadPending(string ticketId)
    {
        var match = _tools.FindLatestStaged(ticketId)
            ?? throw new InvalidOperationException($"No staged item for ticket {ticketId}.");

        if (match.Draft.HumanDecision is not null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} was already reviewed.");
        }

        if (match.Draft.Status != "pending_review")
        {
            throw new InvalidOperationException(
                $"Ticket {ticketId} is not pending review (status: {match.Draft.Status}).");
        }

        return match;
    }
}
