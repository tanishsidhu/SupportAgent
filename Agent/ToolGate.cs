using System.Text.Json;

namespace SupportAgent.Agent;

public static class ToolGate
{
    public static readonly IReadOnlySet<string> AllowedTools = new HashSet<string>(StringComparer.Ordinal)
    {
        "GetTicket",
        "SearchDocs",
        "SearchResolved",
        "StageDraft",
    };

    public static bool IsAllowed(string toolName) => AllowedTools.Contains(toolName);

    public static string Deny(string toolName) =>
        JsonSerializer.Serialize(new
        {
            denied = true,
            error =
                $"Tool '{toolName}' is blocked by policy. " +
                "Only read tools and StageDraft are permitted. Use StageDraft for output.",
        });
}
