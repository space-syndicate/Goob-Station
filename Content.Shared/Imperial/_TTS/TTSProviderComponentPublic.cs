using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.TTS;

[RegisterComponent, NetworkedComponent]
public sealed partial class TTSProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice")]
    public string Voice = default!;
}

