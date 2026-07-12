using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent]
public sealed partial class NanoChatCartridgeComponent : Component
{
    [DataField]
    public NetEntity? UserId;

    [DataField]
    public string? PdaCardName;

    [DataField]
    public int? SelectedChat;

    [DataField]
    public bool NotificationsOn = true;

    [DataField]
    public SoundSpecifier NotificationSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedServer;

    [DataField]
    public Dictionary<int, int> UnreadMessages = new();
}
