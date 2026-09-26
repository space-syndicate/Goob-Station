using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Sound.Components;

/// <summary>
/// Emit ambient while UI is open.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitAmbientOnUIOpenComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityWhitelist Blacklist = new();
}