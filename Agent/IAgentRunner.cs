namespace SupportAgent.Agent;

public interface IAgentRunner
{
    Task<string> RunAsync(string ticketId, CancellationToken cancellationToken = default);
}
