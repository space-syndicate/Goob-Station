using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWormCorpsePossessionSystem))]
public sealed partial class WormCorpseHostComponent : Component
{
    [DataField]
    public EntProtoId EnterAction = "ActionWormCorpseEnter";

    [DataField]
    public EntProtoId ExitAction = "ActionWormCorpseExit";

    [DataField, AutoNetworkedField]
    public EntityUid? EnterActionEntity;

    [DataField]
    public float EnterDelay = 10f;

    [DataField]
    public float EnterCooldown = 180f;

    [DataField]
    public float ExitBleedDamage = 200f;

    [DataField]
    public float Range = 1.5f;

    [DataField]
    public SoundSpecifier? ExitSound = new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg");
}
