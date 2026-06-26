namespace SupportAgent.Agent;

public enum TicketBucket
{
    Answerable,
    NotAQuestion,
    NeedsHuman,
}

public sealed record TicketClassification(TicketBucket Bucket, string Reason)
{
    public string StageStatus => Bucket switch
    {
        TicketBucket.NotAQuestion => "routed",
        TicketBucket.NeedsHuman => "escalated",
        _ => "pending_review",
    };

    public static TicketClassification Parse(string response)
    {
        var json = StripJson(response);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var bucketText = root.GetProperty("bucket").GetString()
            ?? throw new InvalidDataException("Missing bucket in classification JSON.");
        var reason = root.GetProperty("reason").GetString()
            ?? throw new InvalidDataException("Missing reason in classification JSON.");

        return new TicketClassification(ParseBucket(bucketText), reason);
    }

    private static TicketBucket ParseBucket(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "answerable" => TicketBucket.Answerable,
            "not_a_question" => TicketBucket.NotAQuestion,
            "needs_human" => TicketBucket.NeedsHuman,
            _ => throw new InvalidDataException($"Unknown bucket: {value}"),
        };

    private static string StripJson(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```"))
        {
            var lines = text.Split('\n');
            text = string.Join('\n', lines.Skip(1).TakeWhile(line => !line.StartsWith("```")));
        }

        return text.Trim();
    }
}
