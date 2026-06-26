namespace SupportAgent.Agent;

public static class SchedulerOptions
{
    public const int DefaultMaxTicketsPerRun = 10;
    public const int DefaultMaxApiCalls = 80;
}

public sealed record SchedulerRunResult(
    int Processed,
    int Skipped,
    int StoppedByTicketCap,
    int StoppedByApiCap,
    int TotalApiCalls);
