using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
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
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan EnterCooldown = TimeSpan.FromSeconds(180);

    [DataField]
    public DamageSpecifier ExitBleedDamage = new();

    [DataField]
    public float Range = 1.5f;

    [DataField]
    public SoundSpecifier? ExitSound = new SoundPathSpecifier("/Audio/Magic/disintegrate.ogg");

    [DataField]
    public float PossessMinHealthFraction = 0.25f;

    [DataField]
    public ProtoId<DamageTypePrototype> PossessDamageType = "Blunt";
}
