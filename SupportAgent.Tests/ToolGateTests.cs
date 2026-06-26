using OpenAI.Chat;
using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class ToolGateTests
{
    private static string ProjectDataRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));

    [Theory]
    [InlineData("GetTicket")]
    [InlineData("SearchDocs")]
    [InlineData("SearchResolved")]
    [InlineData("StageDraft")]
    public void IsAllowed_PermitsStagingTools(string toolName)
    {
        Assert.True(ToolGate.IsAllowed(toolName));
    }

    [Theory]
    [InlineData("PostToCustomer")]
    [InlineData("DeleteTicket")]
    [InlineData("SendEmail")]
    public void IsAllowed_BlocksUnknownTools(string toolName)
    {
        Assert.False(ToolGate.IsAllowed(toolName));
    }

    [Fact]
    public void Execute_DeniesPostToCustomer()
    {
        var tools = new SupportTools(ProjectDataRoot);
        var call = ChatToolCall.CreateFunctionToolCall(
            "call_post",
            "PostToCustomer",
            BinaryData.FromString("""{"ticket_id":"T-1001","message":"Here is your answer."}"""));

        var result = ToolDispatcher.Execute(tools, call);

        Assert.Contains("\"denied\":true", result);
        Assert.Contains("PostToCustomer", result);
        Assert.DoesNotContain("staged", result);
    }

    [Fact]
    public void Execute_AllowsGetTicketAfterGate()
    {
        var tools = new SupportTools(ProjectDataRoot);
        var call = ChatToolCall.CreateFunctionToolCall(
            "call_1",
            "GetTicket",
            BinaryData.FromString("""{"id":"T-1001"}"""));

        var result = ToolDispatcher.Execute(tools, call);

        Assert.DoesNotContain("\"denied\":true", result);
        Assert.Contains("T-1001", result);
    }
}
