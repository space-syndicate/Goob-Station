using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedWormBloodDrinkSystem))]
public sealed partial class WormBloodDrinkerComponent : Component
{
    [DataField]
    public EntProtoId DrinkAction = "ActionWormBloodDrink";

    [DataField, AutoNetworkedField]
    public EntityUid? DrinkActionEntity;

    [DataField]
    public float InitialDelay = 2f;

    [DataField]
    public float TickDelay = 0.5f;

    [DataField]
    public int DrainAmount = 4;

    [DataField]
    public float Range = 1f;

    [DataField]
    public float ConversionRatio = 0.45f;

    [DataField]
    public float MinVictimBloodFraction = 0.35f;

    [DataField]
    public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg")
    {
        Params = AudioParams.Default.WithVolume(3)
    };
}
