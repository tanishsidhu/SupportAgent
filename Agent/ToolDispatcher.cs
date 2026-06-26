using System.Text.Json;
using OpenAI.Chat;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public static class ToolDispatcher
{
    public static string Execute(SupportTools tools, ChatToolCall toolCall)
    {
        if (!ToolGate.IsAllowed(toolCall.FunctionName))
        {
            return ToolGate.Deny(toolCall.FunctionName);
        }

        using var args = JsonDocument.Parse(toolCall.FunctionArguments);

        return toolCall.FunctionName switch
        {
            "GetTicket" => DispatchGetTicket(tools, args),
            "SearchDocs" => DispatchSearchDocs(tools, args),
            "SearchResolved" => DispatchSearchResolved(tools, args),
            "StageDraft" => DispatchStageDraft(tools, args),
            _ => ToolGate.Deny(toolCall.FunctionName),
        };
    }

    private static string DispatchGetTicket(SupportTools tools, JsonDocument args)
    {
        var id = args.RootElement.GetProperty("id").GetString()!;

        try
        {
            var ticket = tools.GetTicket(id);
            return JsonSerializer.Serialize(ticket);
        }
        catch (KeyNotFoundException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string DispatchSearchDocs(SupportTools tools, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        return JsonSerializer.Serialize(tools.SearchDocs(query));
    }

    private static string DispatchSearchResolved(SupportTools tools, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        return JsonSerializer.Serialize(tools.SearchResolved(query));
    }

    private static string DispatchStageDraft(SupportTools tools, JsonDocument args)
    {
        var root = args.RootElement;
        var record = new StagedDraft(
            root.GetProperty("ticket_id").GetString()!,
            "pending_review",
            root.GetProperty("draft_answer").GetString()!,
            root.GetProperty("citations").EnumerateArray()
                .Select(c => c.GetString()!)
                .ToList(),
            DateTimeOffset.UtcNow);

        tools.StageDraft(record);
        return JsonSerializer.Serialize(new { status = "staged", ticket_id = record.TicketId });
    }
}
