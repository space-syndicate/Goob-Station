using System.Text.Json.Serialization;

namespace Content.Server.Imperial.XxRaay.Systems.LLMNPCSystem.Api;

internal sealed class GroqApiResponse
{
    [JsonPropertyName("choices")]
    public GroqApiChoice[]? Choices { get; set; }
}

internal sealed class GroqApiChoice
{
    [JsonPropertyName("message")]
    public GroqApiMessage? Message { get; set; }
}

internal sealed class GroqApiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
