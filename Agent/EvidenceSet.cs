using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class EvidenceSet
{
    private readonly Dictionary<string, SearchHit> _hits = new(StringComparer.Ordinal);

    public void AddRange(IEnumerable<SearchHit> hits)
    {
        foreach (var hit in hits)
        {
            if (!_hits.TryGetValue(hit.SourceId, out var existing) || hit.Score > existing.Score)
            {
                _hits[hit.SourceId] = hit;
            }
        }
    }

    public IReadOnlyList<SearchHit> All =>
        _hits.Values.OrderByDescending(h => h.Score).ToList();

    public string FormatForPrompt() =>
        All.Count == 0
            ? "(no evidence retrieved)"
            : string.Join("\n\n", All.Select(h => $"[{h.SourceId}]\n{h.Passage}"));
}
