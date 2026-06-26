using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace SupportAgent.Llm;

public sealed class DeepSeekClient : ILlmClient
{
    private readonly ChatClient _chatClient;
    private readonly ApiCallTracker? _apiCalls;

    public DeepSeekClient(DeepSeekSettings settings, ApiCallTracker? apiCalls = null)
    {
        _apiCalls = apiCalls;
        _chatClient = new ChatClient(
            model: DeepSeekSettings.Model,
            credential: new ApiKeyCredential(settings.ApiKey),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(DeepSeekSettings.BaseUrl),
            });
    }

    public ApiCallTracker? ApiCalls => _apiCalls;

    public async Task<string> CompleteChatAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _apiCalls?.RecordCall();

        var completion = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(userMessage)],
            cancellationToken: cancellationToken);

        return completion.Value.Content[0].Text;
    }

    public async Task<string> CompleteAsync(
        IList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        _apiCalls?.RecordCall();

        var completion = await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        return completion.Value.Content[0].Text;
    }

    public async Task<LlmTurnResult> RunTurnAsync(
        IList<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken = default)
    {
        _apiCalls?.RecordCall();

        var completion = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken: cancellationToken);

        var value = completion.Value;
        var text = value.Content.FirstOrDefault(part => part.Kind == ChatMessageContentPartKind.Text)?.Text;

        return new LlmTurnResult(
            value.FinishReason,
            text,
            value.ToolCalls.ToList());
    }
}
