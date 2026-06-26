using OpenAI.Chat;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public sealed class TicketClassifier : ITicketClassifier
{
    private const string SystemPrompt =
        """
        Classify support tickets into exactly one bucket:
        - answerable: general product/API question answerable from docs or past tickets
        - not_a_question: bug report, outage, feature request, or change request (even if it ends with ?)
        - needs_human: specific customer instance with lease/invoice/account ids needing investigation

        Reply with JSON only, no markdown:
        {"bucket":"answerable|not_a_question|needs_human","reason":"one short line"}
        """;

    private readonly DeepSeekClient _client;

    public TicketClassifier(DeepSeekClient client)
    {
        _client = client;
    }

    public async Task<TicketClassification> ClassifyAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        var userMessage =
            $"""
            Ticket {ticket.Id}
            Subject: {ticket.Subject}
            Body: {ticket.Body}
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(userMessage),
        };

        var response = await _client.CompleteAsync(messages, cancellationToken);
        return TicketClassification.Parse(response);
    }
}
