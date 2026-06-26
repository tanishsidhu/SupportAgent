using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class InboxScheduler
{
    private readonly IAgentRunner _loop;
    private readonly SupportTools _tools;
    private readonly DeepSeekClient _client;
    private readonly string _statePath;
    private readonly int _maxTicketsPerRun;
    private readonly int _maxApiCalls;
    private readonly TextWriter _log;

    public InboxScheduler(
        IAgentRunner loop,
        SupportTools tools,
        DeepSeekClient client,
        string statePath,
        int maxTicketsPerRun = SchedulerOptions.DefaultMaxTicketsPerRun,
        int maxApiCalls = SchedulerOptions.DefaultMaxApiCalls,
        TextWriter? log = null)
    {
        _loop = loop;
        _tools = tools;
        _client = client;
        _statePath = statePath;
        _maxTicketsPerRun = maxTicketsPerRun;
        _maxApiCalls = maxApiCalls;
        _log = log ?? Console.Out;
    }

    public async Task<SchedulerRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var state = LoopState.Load(_statePath);
        var pending = GetPendingTickets(state);

        var processed = 0;
        var skipped = _tools.GetAllTickets().Count - pending.Count;
        var stoppedByTicketCap = 0;
        var stoppedByApiCap = 0;

        await _log.WriteLineAsync($"Scheduler: {pending.Count} ticket(s) pending.\n");

        foreach (var ticket in pending)
        {
            if (processed >= _maxTicketsPerRun)
            {
                stoppedByTicketCap = pending.Count - processed;
                await _log.WriteLineAsync(
                    $"Ticket cap reached ({_maxTicketsPerRun}). {stoppedByTicketCap} ticket(s) deferred.\n");
                break;
            }

            if (_client.ApiCalls?.IsAtOrOverLimit(_maxApiCalls) == true)
            {
                stoppedByApiCap = pending.Count - processed;
                await _log.WriteLineAsync(
                    $"API call cap reached ({_maxApiCalls}). {stoppedByApiCap} ticket(s) deferred.\n");
                break;
            }

            var callsBefore = _client.ApiCalls?.Count ?? 0;

            await _log.WriteLineAsync($"========== {ticket.Id}: {ticket.Subject} ==========\n");
            await _loop.RunAsync(ticket.Id, cancellationToken);
            await _log.WriteLineAsync();

            var callsUsed = (_client.ApiCalls?.Count ?? 0) - callsBefore;
            state.MarkCompleted(ticket.Id, callsUsed);
            state.Save(_statePath);
            processed++;
        }

        var result = new SchedulerRunResult(
            processed,
            skipped,
            stoppedByTicketCap,
            stoppedByApiCap,
            _client.ApiCalls?.Count ?? 0);

        await PrintSummaryAsync(result);
        ReviewQueueView.Print(_tools, _log);

        return result;
    }

    private List<Ticket> GetPendingTickets(LoopState state)
    {
        return _tools.GetAllTickets()
            .Where(ticket => !IsAlreadyHandled(ticket.Id, state))
            .ToList();
    }

    private bool IsAlreadyHandled(string ticketId, LoopState state)
    {
        if (state.IsCompleted(ticketId))
        {
            return true;
        }

        return _tools.FindLatestStaged(ticketId) is not null;
    }

    private async Task PrintSummaryAsync(SchedulerRunResult result)
    {
        await _log.WriteLineAsync("=== Scheduler summary ===");
        await _log.WriteLineAsync($"Processed: {result.Processed}");
        await _log.WriteLineAsync($"Skipped (already done): {result.Skipped}");

        if (result.StoppedByTicketCap > 0)
        {
            await _log.WriteLineAsync($"Deferred (ticket cap): {result.StoppedByTicketCap}");
        }

        if (result.StoppedByApiCap > 0)
        {
            await _log.WriteLineAsync($"Deferred (API cap): {result.StoppedByApiCap}");
        }

        await _log.WriteLineAsync($"Total API calls this run: {result.TotalApiCalls}");
        await _log.WriteLineAsync($"State saved: {_statePath}\n");
    }
}
