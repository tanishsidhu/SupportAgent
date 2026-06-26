using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Ui;

public sealed record ReviewQueueItem(
    string TicketId,
    string Subject,
    string Question,
    StagedDraft Draft,
    IReadOnlyList<ActivityEvent> Pipeline);

public static class ReviewQueueBuilder
{
    public static IReadOnlyList<ReviewQueueItem> GetPending(SupportTools tools)
    {
        return tools.ListStagedDrafts()
            .Where(draft => draft.Status == "pending_review" && draft.HumanDecision is null)
            .Select(draft =>
            {
                var trace = TraceReader.LoadLatest(tools.TracesDir, draft.TicketId);
                var enriched = DraftEnricher.Enrich(draft, trace);
                var ticket = tools.GetTicket(draft.TicketId);
                return new ReviewQueueItem(
                    draft.TicketId,
                    ticket.Subject,
                    ticket.Body,
                    enriched,
                    AgentDashboard.BuildPipelineSteps(trace));
            })
            .OrderBy(item => item.TicketId, StringComparer.Ordinal)
            .ToList();
    }
}
