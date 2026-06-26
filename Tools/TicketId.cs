using System.Text.RegularExpressions;

namespace SupportAgent.Tools;

public static partial class TicketId
{
    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex AllowedPattern();

    public static bool IsValid(string? ticketId) =>
        ticketId is not null && AllowedPattern().IsMatch(ticketId);

    public static string Require(string? ticketId)
    {
        if (!IsValid(ticketId))
        {
            throw new ArgumentException(
                $"Invalid ticket id: '{ticketId}'. Allowed: letters, digits, dash, underscore (max 64).");
        }

        return ticketId!;
    }
}
