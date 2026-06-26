using OpenAI.Chat;

namespace SupportAgent.Agent;

public static class AgentToolDefinitions
{
    public static IReadOnlyList<ChatTool> All { get; } =
    [
        ChatTool.CreateFunctionTool(
            functionName: "GetTicket",
            functionDescription: "Load one inbox ticket by id.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string", "description": "Ticket id, e.g. T-1001" }
                  },
                  "required": ["id"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: "SearchDocs",
            functionDescription: "Search CapStream documentation pages.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "query": { "type": "string", "description": "Search query" }
                  },
                  "required": ["query"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: "SearchResolved",
            functionDescription: "Search previously resolved support tickets.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "query": { "type": "string", "description": "Search query" }
                  },
                  "required": ["query"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: "StageDraft",
            functionDescription: "Stage a draft answer for human review. Never posts to customers.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "ticket_id": { "type": "string" },
                    "draft_answer": { "type": "string" },
                    "citations": {
                      "type": "array",
                      "items": { "type": "string" }
                    }
                  },
                  "required": ["ticket_id", "draft_answer", "citations"]
                }
                """)),
    ];

    /// <summary>
    /// Decoy tool for the Phase 4 demo. Exposed to the model in demo mode only; always blocked by the gate.
    /// </summary>
    public static ChatTool PostToCustomer { get; } = ChatTool.CreateFunctionTool(
        functionName: "PostToCustomer",
        functionDescription: "Post the reply directly to the customer in Jira.",
        functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "ticket_id": { "type": "string" },
                "message": { "type": "string" }
              },
              "required": ["ticket_id", "message"]
            }
            """));
}
