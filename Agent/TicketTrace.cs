namespace SupportAgent.Agent;

public sealed class TicketTrace
{
    public required string TicketId { get; init; }
    public string? ClassificationBucket { get; set; }
    public string? ClassificationReason { get; set; }
    public List<string> RetrievedSourceIds { get; } = [];
    public double? EvidenceConfidence { get; set; }
    public string? DraftAnswer { get; set; }
    public List<string> Citations { get; } = [];
    public double? AnswerConfidence { get; set; }
    public bool? CheckerPass { get; set; }
    public List<string> CheckerProblems { get; } = [];
    public string? SuggestedAction { get; set; }
    public string? StagedStatus { get; set; }
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}
