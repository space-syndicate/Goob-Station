using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.DataDefinitions;

[DataDefinition]
public sealed class LLMMessageHistoryItem
{
    [DataField]
    public string Role { get; set; } = "";

    [DataField]
    public string Content { get; set; } = "";
}
