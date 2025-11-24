using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Vampire;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class GhoulComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Master;

    [DataField, AutoNetworkedField]
    public float BloodDamage = 0f;

    [DataField]
    public float CritThreshold = 100f;

    [DataField]
    public TimeSpan BloodDecayInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public float BloodDecayAmount = 100f;

    [DataField]
    public TimeSpan NextBloodDecay = TimeSpan.Zero;

    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "BloodGhoul";

    [DataField]
    public TimeSpan ShakingTime = TimeSpan.FromSeconds(5);
}
