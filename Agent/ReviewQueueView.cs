using SupportAgent.Tools;

namespace SupportAgent.Agent;

public static class ReviewQueueView
{
    public static void Print(SupportTools tools, TextWriter? output = null)
    {
        output ??= Console.Out;
        var items = tools.ListStagedDrafts();

        if (items.Count == 0)
        {
            output.WriteLine("Review queue is empty.");
            return;
        }

        output.WriteLine("=== Review queue ===\n");

        foreach (var item in items)
        {
            PrintItem(item, output);
            output.WriteLine(new string('-', 60));
        }
    }

    private static void PrintItem(StagedDraft item, TextWriter output)
    {
        if (item.Status == "pending_review" && item.HumanDecision is null)
        {
            output.WriteLine("AI DRAFT — pending review");
        }

        output.WriteLine($"Ticket:   {item.TicketId}");
        output.WriteLine($"Status:   {item.Status}");

        if (item.HumanDecision is not null)
        {
            output.WriteLine($"Decision: {item.HumanDecision} at {item.ReviewedAt:u}");
        }

        output.WriteLine($"Draft:    {item.DraftAnswer}");

        if (item.Citations.Count > 0)
        {
            output.WriteLine($"Citations: {string.Join(", ", item.Citations)}");
        }

        if (item.EvidenceConfidence is not null)
        {
            output.WriteLine($"Evidence confidence: {item.EvidenceConfidence:F2}");
        }

        if (item.AnswerConfidence is not null)
        {
            output.WriteLine($"Answer confidence:   {item.AnswerConfidence:F2}");
        }

        if (item.CheckerPass is not null)
        {
            output.WriteLine(
                $"Checker: pass={item.CheckerPass}, suggested={item.SuggestedAction ?? "n/a"}");

            if (item.CheckerProblems is { Count: > 0 })
            {
                output.WriteLine("Checker problems:");
                foreach (var problem in item.CheckerProblems)
                {
                    output.WriteLine($"  - {problem}");
                }
            }
        }

        if (item.HumanDecision == "amended" && item.FinalAnswer is not null)
        {
            output.WriteLine($"Approved answer: {item.FinalAnswer}");
            output.WriteLine($"(Original draft: {item.OriginalDraftAnswer})");
        }
    }
}
