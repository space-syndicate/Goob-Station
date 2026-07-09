using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent]
public sealed partial class NanoChatCartridgeComponent : Component
{
    [DataField]
    public string? PdaCardName;

    [DataField]
    public string? SelectedContact;

    [DataField]
    public bool NotificationsOn = true;

    [DataField]
    public Dictionary<string, List<NanoChatMessage>> ChatHistories = new();
}
