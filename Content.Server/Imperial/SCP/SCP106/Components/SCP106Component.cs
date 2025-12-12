using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage.Prototypes;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106Component : Component
{
    [DataField("pocketDimensionMap", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath PocketMapPath = new("/Maps/Imperial/Maps/SCP/STARIC.yml");

    [ViewVariables]
    public MapId PocketMapId { get; internal set; }

    [DataField("puddleSpawnAction")]
    public EntProtoId PuddleSpawnAction = "ActionSCP106SpawnPuddle";

    [DataField("puddleId")]
    public EntProtoId PuddleID = "PortalSCP106";

    [AutoNetworkedField, DataField("puddleSpawnActionEntity")]
    public EntityUid? PuddleSpawnActionEntity;

    [DataField("warpId")]
    public EntProtoId WarpID = "PocketDimensionWarp";

    [AutoNetworkedField]
    public EntityUid? LastAttackedPerson;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier FirstHitDamage = new();

    [DataField("teleportSound")]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_local.ogg");

    [DataField("globalTeleportSound")]
    public SoundSpecifier GlobalTeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_global.ogg");

    [DataField("puddleSpawnSound")]
    public SoundSpecifier PuddleSpawnSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_puddle_spawn.ogg");
    [DataField("puddleExitSound")]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_puddle_exit.ogg");
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 3f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxPuddles")]
    public int MaxPuddles = 5;

    [ViewVariables]
    public List<EntityUid> Puddles { get; set; } = new();
    [ViewVariables]
    public ProtoId<PolymorphPrototype> GhostMorph = "SCP106GhostMorph";
    [ViewVariables]
    public bool InDimension = false;
    [ViewVariables]
    public bool InPocketDimension = false;
    [ViewVariables]
    public EntityCoordinates? PastPosition { get; set;} 
    [DataField(readOnly: true)]
    public EntProtoId? RemoteEntityProto = "ImperialSCP106Ghost";
    [DataField(readOnly: true)]
    public EntProtoId? OriginalEntityProto = "ImperialSCPDed";
    [DataField("prototypeScream", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string PrototypeScream = "Scream";


    [DataField("puddleEnterDimensionAction")]
    public EntProtoId PuddleEnterDimensionAction = "ActionSCP106GoIntoDimension";
    [AutoNetworkedField, DataField("puddleEnterDimensionActionEntity")]
    public EntityUid? PuddleEnterDimensionEntity;
    [DataField("puddleExitDimensionAction")]
    public EntProtoId PuddleExitDimensionAction = "ActionSCP106ExitDimension";
    [AutoNetworkedField, DataField("puddleExitDimensionEntity")]
    public EntityUid? PuddleExitDimensionEntity;
    [ViewVariables]
    public MapId PastMapId { get; internal set; }
    [DataField("laughSound")]
    public SoundSpecifier LaughSound = new SoundPathSpecifier("/Audio/Imperial/Seriozha/SCP/106/Laugh.ogg");
    [DataField("minDistance")]
    public float MinDistance = 15.0f;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamagePerSecond = new();
}
