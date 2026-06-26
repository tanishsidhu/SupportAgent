namespace SupportAgent.Agent;

/// <summary>
/// Expected classification buckets for the synthetic inbox (used as documentation and test reference).
/// </summary>
public static class ExpectedInboxBuckets
{
    public static IReadOnlyDictionary<string, TicketBucket> ByTicketId { get; } =
        new Dictionary<string, TicketBucket>
        {
            ["T-1001"] = TicketBucket.Answerable,
            ["T-1002"] = TicketBucket.Answerable,
            ["T-1003"] = TicketBucket.Answerable,
            ["T-1004"] = TicketBucket.Answerable,
            ["T-1005"] = TicketBucket.Answerable,
            ["T-1006"] = TicketBucket.NotAQuestion,
            ["T-1007"] = TicketBucket.NotAQuestion,
            ["T-1008"] = TicketBucket.NotAQuestion,
            ["T-1009"] = TicketBucket.NeedsHuman,
            ["T-1010"] = TicketBucket.Answerable,
        };
}
