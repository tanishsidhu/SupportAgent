namespace SupportAgent.Tools;

public sealed record Ticket(string Id, string Subject, string Body);

public sealed record SearchHit(string SourceId, string Passage, double Score);

public sealed record StagedDraft(
    string TicketId,
    string Status,
    string DraftAnswer,
    IReadOnlyList<string> Citations,
    DateTimeOffset StagedAt,
    double? EvidenceConfidence = null,
    double? AnswerConfidence = null,
    bool? CheckerPass = null,
    IReadOnlyList<string>? CheckerProblems = null,
    string? SuggestedAction = null,
    string? HumanDecision = null,
    string? FinalAnswer = null,
    string? OriginalDraftAnswer = null,
    DateTimeOffset? ReviewedAt = null);
