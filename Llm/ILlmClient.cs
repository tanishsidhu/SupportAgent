namespace SupportAgent.Llm;

/// <summary>
/// Chat completion interface for the agent loop. Implemented by DeepSeekClient.
/// </summary>
public interface ILlmClient
{
    Task<string> CompleteChatAsync(string userMessage, CancellationToken cancellationToken = default);
}
