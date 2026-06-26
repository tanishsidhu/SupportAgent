namespace SupportAgent.Ui;

public sealed class AgentRunState
{
    public bool IsRunning { get; set; }
    public string Log { get; set; } = "Agent starting…";
    public string? Error { get; set; }
}
