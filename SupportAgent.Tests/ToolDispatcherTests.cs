using OpenAI.Chat;
using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Tests;

public class ToolDispatcherTests
{
    private static string ProjectDataRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));

    [Fact]
    public void Dispatch_GetTicket_ReturnsTicketJson()
    {
        var tools = new SupportTools(ProjectDataRoot);
        var call = ChatToolCall.CreateFunctionToolCall(
            "call_1",
            "GetTicket",
            BinaryData.FromString("""{"id":"T-1001"}"""));

        var result = ToolDispatcher.Execute(tools, call);

        Assert.Contains("T-1001", result);
        Assert.Contains("idempotent", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SearchDocs_ReturnsHits()
    {
        var tools = new SupportTools(ProjectDataRoot);
        var call = ChatToolCall.CreateFunctionToolCall(
            "call_2",
            "SearchDocs",
            BinaryData.FromString("""{"query":"idempotency key"}"""));

        var result = ToolDispatcher.Execute(tools, call);

        Assert.Contains("CONF-1009", result);
    }
}
