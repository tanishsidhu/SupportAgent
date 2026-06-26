using OpenAI.Chat;
using SupportAgent.Llm;
using SupportAgent.Tools;

namespace SupportAgent.Agent;

public interface IDraftMaker
{
    Task<DraftResult> DraftAsync(
        Ticket ticket,
        EvidenceSet evidence,
        IReadOnlyList<string>? revisionNotes,
        CancellationToken cancellationToken = default);
}

public sealed class DraftMaker : IDraftMaker
{
    private readonly DeepSeekClient _client;

    public DraftMaker(DeepSeekClient client)
    {
        _client = client;
    }

    public async Task<DraftResult> DraftAsync(
        Ticket ticket,
        EvidenceSet evidence,
        IReadOnlyList<string>? revisionNotes,
        CancellationToken cancellationToken = default)
    {
        var revisionBlock = revisionNotes is { Count: > 0 }
            ? $"\nFix these checker problems:\n- {string.Join("\n- ", revisionNotes)}"
            : string.Empty;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You draft grounded support answers. Use ONLY the provided evidence.
                Cite source ids inline. Reply JSON only:
                {"draft_answer":"...","citations":["CONF-1009"],"answer_confidence":0.0}
                answer_confidence is how confident you are in the drafted response (0.0-1.0).
                """),
            new UserChatMessage(
                $"""
                Ticket: {ticket.Subject}
                Question: {ticket.Body}

                Evidence:
                {evidence.FormatForPrompt()}
                {revisionBlock}
                """),
        };

        var response = await _client.CompleteAsync(messages, cancellationToken);
        return DraftResult.Parse(response);
    }
}

public interface IDraftChecker
{
    Task<CheckerVerdict> VerifyAsync(
        Ticket ticket,
        DraftResult draft,
        EvidenceSet evidence,
        CancellationToken cancellationToken = default);
}

public sealed class DraftChecker : IDraftChecker
{
    private readonly DeepSeekClient _client;

    public DraftChecker(DeepSeekClient client)
    {
        _client = client;
    }

    public async Task<CheckerVerdict> VerifyAsync(
        Ticket ticket,
        DraftResult draft,
        EvidenceSet evidence,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You verify support drafts against evidence. Be strict.
                Check: every claim grounded in cited sources; responsive to the question;
                answer_confidence honest vs evidence quality.
                If sources conflict, fail the draft.
                Reply JSON only:
                {"pass":true,"problems":[],"suggested_action":"post"}
                suggested_action is one of: post, amend, escalate
                """),
            new UserChatMessage(
                $"""
                Ticket: {ticket.Subject}
                Question: {ticket.Body}

                Evidence:
                {evidence.FormatForPrompt()}

                Draft:
                {draft.DraftAnswer}

                Citations: {string.Join(", ", draft.Citations)}
                Stated answer confidence: {draft.AnswerConfidence:F2}
                """),
        };

        var response = await _client.CompleteAsync(messages, cancellationToken);
        return CheckerVerdict.Parse(response);
    }
}
