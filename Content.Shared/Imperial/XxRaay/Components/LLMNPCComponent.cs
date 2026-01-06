using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Component for NPC that uses LLM to generate chat responses.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LLMNPCComponent : Component
{
    [DataField]
    public string ApiKey = "";

    [DataField]
    public string SystemPrompt = "";

    [DataField]
    public string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    [DataField]
    public string Model = "llama-3.1-8b-instant";

    [DataField]
    public float MaxDistanceTiles = 5.0f;

    [DataField]
    public int MaxHistoryMessages = 10;

    [DataField]
    public bool IncludeContextInfo = false;

    [DataField]
    public string NPCName = "";

    [NonSerialized]
    public bool IsGenerating = false;

    [NonSerialized]
    public List<LLMMessageHistoryItem> MessageHistory = new();
}

public sealed class LLMMessageHistoryItem
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

