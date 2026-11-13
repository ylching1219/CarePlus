namespace CarePlus.API.Services.ChatServices;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.15;
    public int MaxCompletionTokens { get; set; } = 600;
}

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public string Url { get; set; } = "http://localhost:6333";
    public string ApiKey { get; set; } = string.Empty;
    public string Collection { get; set; } = "careplus_knowledge";
    public string InferenceModel { get; set; } = "sentence-transformers/all-minilm-l6-v2";
    public int VectorSize { get; set; } = 384;
    public int SearchLimit { get; set; } = 5;
    public double MinimumScore { get; set; } = 0.35;
}

public sealed class ChatOptions
{
    public const string SectionName = "Chat";
    public int MaximumHistoryMessages { get; set; } = 6;
    public int MaximumContextCharacters { get; set; } = 8_000;
    public string Disclaimer { get; set; } = "CarePlus chat provides general clinic information and does not replace professional medical advice.";
}

