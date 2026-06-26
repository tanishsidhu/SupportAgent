namespace SupportAgent.Agent;

public sealed record EvidenceAssessment(double Score, string Reason)
{
    public static EvidenceAssessment Parse(string response)
    {
        var json = JsonHelper.StripMarkdownJson(response);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new EvidenceAssessment(
            root.GetProperty("score").GetDouble(),
            root.GetProperty("reason").GetString() ?? string.Empty);
    }
}

public sealed record DraftResult(
    string DraftAnswer,
    IReadOnlyList<string> Citations,
    double AnswerConfidence)
{
    public static DraftResult Parse(string response)
    {
        var json = JsonHelper.StripMarkdownJson(response);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var citations = root.GetProperty("citations").EnumerateArray()
            .Select(c => c.GetString()!)
            .ToList();

        return new DraftResult(
            root.GetProperty("draft_answer").GetString()!,
            citations,
            root.GetProperty("answer_confidence").GetDouble());
    }
}

public sealed record CheckerVerdict(
    bool Pass,
    IReadOnlyList<string> Problems,
    string SuggestedAction)
{
    public static CheckerVerdict Parse(string response)
    {
        var json = JsonHelper.StripMarkdownJson(response);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var problems = root.TryGetProperty("problems", out var problemsElement)
            ? problemsElement.EnumerateArray().Select(p => p.GetString()!).ToList()
            : [];

        return new CheckerVerdict(
            root.GetProperty("pass").GetBoolean(),
            problems,
            root.GetProperty("suggested_action").GetString() ?? "escalate");
    }
}

internal static class JsonHelper
{
    public static string StripMarkdownJson(string text)
    {
        text = text.Trim();
        if (!text.StartsWith("```"))
        {
            return text;
        }

        var lines = text.Split('\n');
        return string.Join('\n', lines.Skip(1).TakeWhile(line => !line.StartsWith("```"))).Trim();
    }
}
