using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class TicketIdTests
{
    [Theory]
    [InlineData("T-1001")]
    [InlineData("RES_2001")]
    [InlineData("abc123")]
    public void IsValid_AcceptsNormalIds(string id)
    {
        Assert.True(TicketId.IsValid(id));
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("T-1001/../secret")]
    [InlineData("*")]
    [InlineData("T 1001")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RejectsTraversalAndGlobs(string? id)
    {
        Assert.False(TicketId.IsValid(id));
    }

    [Fact]
    public void Require_ThrowsOnInvalidId()
    {
        Assert.Throws<ArgumentException>(() => TicketId.Require("../oops"));
    }
}
