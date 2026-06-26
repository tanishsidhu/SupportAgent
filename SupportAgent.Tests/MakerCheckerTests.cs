using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class MakerCheckerModelTests
{
    [Fact]
    public void Parse_CheckerVerdict_RejectsDraft()
    {
        var verdict = CheckerVerdict.Parse(
            """
            {"pass":false,"problems":["24h vs 48h conflict not resolved"],"suggested_action":"amend"}
            """);

        Assert.False(verdict.Pass);
        Assert.Contains("24h", verdict.Problems[0]);
        Assert.Equal("amend", verdict.SuggestedAction);
    }

    [Fact]
    public void Parse_DraftResult_IncludesConfidence()
    {
        var draft = DraftResult.Parse(
            """
            {"draft_answer":"Use Idempotency-Key.","citations":["CONF-1009"],"answer_confidence":0.9}
            """);

        Assert.Equal(0.9, draft.AnswerConfidence);
        Assert.Equal("CONF-1009", draft.Citations[0]);
    }
}

public class EvidenceSetTests
{
    [Fact]
    public void AddRange_KeepsHighestScorePerSource()
    {
        var set = new EvidenceSet();
        set.AddRange(
        [
            new SearchHit("CONF-1009", "first", 1.0),
            new SearchHit("CONF-1009", "second", 2.0),
        ]);

        Assert.Single(set.All);
        Assert.Equal(2.0, set.All[0].Score);
    }
}
