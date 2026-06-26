using SupportAgent.Tools;

namespace SupportAgent.Agent;

public interface IEvidenceRetriever
{
    Task<(EvidenceSet Evidence, double Confidence)> RetrieveAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);
}
