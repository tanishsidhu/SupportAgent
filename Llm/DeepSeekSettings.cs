namespace SupportAgent.Llm;

public sealed record DeepSeekSettings(string ApiKey)
{
    public const string BaseUrl = "https://api.deepseek.com";
    public const string Model = "deepseek-chat";

    public static DeepSeekSettings FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "DEEPSEEK_API_KEY is not set. Export it before running.");
        }

        return new DeepSeekSettings(apiKey);
    }
}
