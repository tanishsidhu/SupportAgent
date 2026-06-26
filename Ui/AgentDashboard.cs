using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Ui;

public sealed record InboxTicketStatus(
    string TicketId,
    string Subject,
    string Phase,
    string PhaseLabel,
    DateTimeOffset? LastActivityAt);

public sealed record ActivityEvent(
    string TicketId,
    string Step,
    string Detail,
    DateTimeOffset At);

public sealed record AgentStatusSnapshot(
    DateTimeOffset? LastSchedulerRunAt,
    int InboxTotal,
    int PendingAgent,
    int InReview,
    int Routed,
    int Escalated,
    int Reviewed,
    IReadOnlyList<InboxTicketStatus> Tickets,
    IReadOnlyList<ActivityEvent> RecentActivity,
    bool AgentRecentlyActive,
    bool AgentRunning,
    string? AgentLog,
    string? AgentError);

public static class AgentDashboard
{
    private static readonly TimeSpan RecentActivityWindow = TimeSpan.FromMinutes(2);

    public static AgentStatusSnapshot Build(
        SupportTools tools,
        string loopStatePath,
        AgentRunState? runState = null)
    {
        var state = LoopState.Load(loopStatePath);
        var traces = TraceReader.LoadAll(tools.TracesDir);
        var tracesByTicket = traces
            .GroupBy(trace => trace.TicketId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var stagedByTicket = tools.ListStagedDrafts()
            .GroupBy(draft => draft.TicketId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(d => d.StagedAt).First(),
                StringComparer.Ordinal);

        var tickets = tools.GetAllTickets()
            .Select(ticket =>
            {
                stagedByTicket.TryGetValue(ticket.Id, out var staged);
                tracesByTicket.TryGetValue(ticket.Id, out var trace);
                var (phase, label) = ResolvePhase(staged, state, ticket.Id);
                var lastAt = staged?.ReviewedAt
                    ?? staged?.StagedAt
                    ?? trace?.CompletedAt
                    ?? state.Completed.FirstOrDefault(entry => entry.TicketId == ticket.Id)?.CompletedAt;

                return new InboxTicketStatus(ticket.Id, ticket.Subject, phase, label, lastAt);
            })
            .OrderBy(status => status.TicketId, StringComparer.Ordinal)
            .ToList();

        var recentActivity = BuildActivityFeed(traces);
        var agentRecentlyActive =
            runState?.IsRunning == true ||
            (state.LastRunAt is not null &&
             DateTimeOffset.UtcNow - state.LastRunAt.Value <= RecentActivityWindow);

        return new AgentStatusSnapshot(
            state.LastRunAt,
            tickets.Count,
            tickets.Count(t => t.Phase == "pending"),
            tickets.Count(t => t.Phase == "review"),
            tickets.Count(t => t.Phase == "routed"),
            tickets.Count(t => t.Phase == "escalated"),
            tickets.Count(t => t.Phase is "approved" or "amended" or "rejected"),
            tickets,
            recentActivity,
            agentRecentlyActive,
            runState?.IsRunning ?? false,
            runState?.Log,
            runState?.Error);
    }

    public static IReadOnlyList<ActivityEvent> BuildPipelineSteps(TicketTrace? trace)
    {
        if (trace is null)
        {
            return [];
        }

        var events = new List<ActivityEvent>();

        if (trace.ClassificationBucket is not null)
        {
            events.Add(new ActivityEvent(
                trace.TicketId,
                "classify",
                $"{trace.ClassificationBucket}: {trace.ClassificationReason}",
                trace.CompletedAt));
        }

        if (trace.RetrievedSourceIds.Count > 0)
        {
            events.Add(new ActivityEvent(
                trace.TicketId,
                "retrieve",
                $"{trace.RetrievedSourceIds.Count} sources · evidence {Format(trace.EvidenceConfidence)}",
                trace.CompletedAt));
        }

        if (trace.DraftAnswer is not null)
        {
            events.Add(new ActivityEvent(
                trace.TicketId,
                "draft",
                $"Answer confidence {Format(trace.AnswerConfidence)}",
                trace.CompletedAt));
        }

        if (trace.CheckerPass is not null)
        {
            events.Add(new ActivityEvent(
                trace.TicketId,
                "check",
                $"Pass={trace.CheckerPass} · {trace.SuggestedAction ?? "n/a"}",
                trace.CompletedAt));
        }

        if (trace.StagedStatus is not null)
        {
            events.Add(new ActivityEvent(
                trace.TicketId,
                "stage",
                trace.StagedStatus,
                trace.CompletedAt));
        }

        return events;
    }

    private static (string Phase, string Label) ResolvePhase(
        StagedDraft? staged,
        LoopState state,
        string ticketId)
    {
        if (staged is null)
        {
            return ("pending", "Waiting");
        }

        if (staged.HumanDecision is not null)
        {
            return staged.HumanDecision switch
            {
                "approved" => ("approved", "Approved"),
                "amended" => ("amended", "Amended"),
                "rejected" => ("rejected", "Rejected"),
                _ => ("reviewed", "Reviewed"),
            };
        }

        return staged.Status switch
        {
            "pending_review" => ("review", "In review"),
            "routed" => ("routed", "Routed"),
            "escalated" => ("escalated", "Escalated"),
            _ => ("review", staged.Status),
        };
    }

    private static IReadOnlyList<ActivityEvent> BuildActivityFeed(IReadOnlyList<TicketTrace> traces)
    {
        return traces
            .SelectMany(trace => BuildPipelineSteps(trace))
            .OrderByDescending(evt => evt.At)
            .Take(40)
            .ToList();
    }

    private static string Format(double? value) =>
        value is null ? "n/a" : value.Value.ToString("F2");
}
