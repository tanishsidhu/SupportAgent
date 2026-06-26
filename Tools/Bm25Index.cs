using System.Text.RegularExpressions;

namespace SupportAgent.Tools;

public sealed class Bm25Index : IRetrievalIndex
{
    private const double K1 = 1.2;
    private const double B = 0.75;

    private readonly List<IndexedDoc> _docs;
    private readonly double _avgLength;
    private readonly Dictionary<string, int> _documentFrequency;

    public Bm25Index(IEnumerable<(string Id, string Text)> documents)
    {
        _docs = documents
            .Select(d => new IndexedDoc(d.Id, d.Text, TermFrequencies(d.Text)))
            .ToList();

        _avgLength = _docs.Count == 0 ? 0 : _docs.Average(d => d.Length);
        _documentFrequency = BuildDocumentFrequency(_docs);
    }

    public IReadOnlyList<SearchHit> Search(string query, int maxResults = 5)
    {
        var terms = Tokenize(query);
        if (terms.Count == 0 || _docs.Count == 0)
        {
            return [];
        }

        return _docs
            .Select(doc => new SearchHit(doc.Id, doc.Text, Score(doc, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .Take(maxResults)
            .ToList();
    }

    private double Score(IndexedDoc doc, IReadOnlyList<string> queryTerms)
    {
        var score = 0.0;

        foreach (var term in queryTerms.Distinct())
        {
            if (!doc.TermCounts.TryGetValue(term, out var termFrequency))
            {
                continue;
            }

            var idf = Idf(term);
            var numerator = termFrequency * (K1 + 1);
            var denominator = termFrequency + K1 * (1 - B + B * doc.Length / _avgLength);
            score += idf * numerator / denominator;
        }

        return score;
    }

    private double Idf(string term)
    {
        var docCount = _documentFrequency.GetValueOrDefault(term, 0);
        if (docCount == 0)
        {
            return 0;
        }

        return Math.Log(1 + (_docs.Count - docCount + 0.5) / (docCount + 0.5));
    }

    private static Dictionary<string, int> BuildDocumentFrequency(IEnumerable<IndexedDoc> docs)
    {
        var frequencies = new Dictionary<string, int>();

        foreach (var doc in docs)
        {
            foreach (var term in doc.TermCounts.Keys)
            {
                frequencies[term] = frequencies.GetValueOrDefault(term) + 1;
            }
        }

        return frequencies;
    }

    private static Dictionary<string, int> TermFrequencies(string text)
    {
        var counts = new Dictionary<string, int>();

        foreach (var term in Tokenize(text))
        {
            counts[term] = counts.GetValueOrDefault(term) + 1;
        }

        return counts;
    }

    private static List<string> Tokenize(string text)
    {
        return Regex
            .Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(token => token.Length > 1)
            .ToList();
    }

    private sealed record IndexedDoc(string Id, string Text, Dictionary<string, int> TermCounts)
    {
        public int Length => TermCounts.Values.Sum();
    }
}
