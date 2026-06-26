using SupportAgent.Tools;

namespace SupportAgent.Agent;

public static class InboxRunner
{
    public static async Task RunAsync(AgentLoop loop, SupportTools tools, TextWriter? log = null)
    {
        log ??= Console.Out;

        foreach (var ticket in tools.GetAllTickets())
        {
            await log.WriteLineAsync($"========== {ticket.Id}: {ticket.Subject} ==========");
            await log.WriteLineAsync();
            await loop.RunAsync(ticket.Id);
            await log.WriteLineAsync();
        }

        await log.WriteLineAsync();
        ReviewQueueView.Print(tools, log);
    }
}
