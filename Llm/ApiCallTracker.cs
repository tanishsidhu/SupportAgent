namespace SupportAgent.Llm;

public sealed class ApiCallTracker
{
    public int Count { get; private set; }

    public void RecordCall() => Count++;

    public void Reset() => Count = 0;

    public bool IsAtOrOverLimit(int maxCalls) => Count >= maxCalls;
}
