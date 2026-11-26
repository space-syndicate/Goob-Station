using Content.Shared.Sound.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Sound.Components;

/// <summary>
/// Emit sound when UI is closed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitSoundOnUICloseComponent : BaseEmitSoundComponent
{
    [DataField]
    public EntityWhitelist Blacklist = new();
}