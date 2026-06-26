using OpenAI.Chat;

namespace SupportAgent.Llm;

public sealed record LlmTurnResult(
    ChatFinishReason FinishReason,
    string? Text,
    IReadOnlyList<ChatToolCall> ToolCalls);
