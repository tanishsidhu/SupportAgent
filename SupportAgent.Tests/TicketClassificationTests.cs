using SupportAgent.Agent;

namespace SupportAgent.Tests;

public class TicketClassificationTests
{
    [Fact]
    public void Parse_AcceptsPlainJson()
    {
        var result = TicketClassification.Parse(
            """{"bucket":"answerable","reason":"General API question."}""");

        Assert.Equal(TicketBucket.Answerable, result.Bucket);
        Assert.Equal("General API question.", result.Reason);
        Assert.Equal("pending_review", result.StageStatus);
    }

    [Fact]
    public void Parse_AcceptsMarkdownWrappedJson()
    {
        var result = TicketClassification.Parse(
            """
            ```json
            {"bucket":"not_a_question","reason":"Bug report."}
            ```
            """);

        Assert.Equal(TicketBucket.NotAQuestion, result.Bucket);
        Assert.Equal("routed", result.StageStatus);
    }

    [Theory]
    [InlineData("needs_human", TicketBucket.NeedsHuman, "escalated")]
    [InlineData("not_a_question", TicketBucket.NotAQuestion, "routed")]
    public void StageStatus_MapsBucket(string bucket, TicketBucket expected, string status)
    {
        var result = TicketClassification.Parse(
            $$"""{"bucket":"{{bucket}}","reason":"test"}""");

        Assert.Equal(expected, result.Bucket);
        Assert.Equal(status, result.StageStatus);
    }
}
