using SupportAgent.Tools;

namespace SupportAgent.Agent;

public interface ITicketClassifier
{
    Task<TicketClassification> ClassifyAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
