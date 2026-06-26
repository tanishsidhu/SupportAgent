using System.Text.Json;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public static class TraceWriter
{
    public static string Save(TicketTrace trace, string tracesDir)
    {
        Directory.CreateDirectory(tracesDir);

        var fileName = $"{trace.TicketId}-{trace.CompletedAt:yyyyMMddHHmmss}.json";
        var path = Path.Combine(tracesDir, fileName);

        var json = JsonSerializer.Serialize(trace, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        return path;
    }

    public static void PrintSummary(TicketTrace trace, TextWriter output)
    {
        output.WriteLine($"Trace: {trace.TicketId}");
        output.WriteLine($"  Classification: {trace.ClassificationBucket} — {trace.ClassificationReason}");

        if (trace.RetrievedSourceIds.Count > 0 && trace.EvidenceConfidence is not null)
        {
            output.WriteLine(
                $"  Retrieved: {string.Join(", ", trace.RetrievedSourceIds)} (evidence confidence {trace.EvidenceConfidence:F2})");
        }

        if (trace.DraftAnswer is not null)
        {
            output.WriteLine($"  Draft: {trace.DraftAnswer}");
            output.WriteLine(
                $"  Citations: {string.Join(", ", trace.Citations)} (answer confidence {trace.AnswerConfidence:F2})");
        }

        if (trace.CheckerPass is not null)
        {
            output.WriteLine(
                $"  Checker: pass={trace.CheckerPass}, action={trace.SuggestedAction}");
        }

        output.WriteLine($"  Staged: {trace.StagedStatus}");
    }
}
