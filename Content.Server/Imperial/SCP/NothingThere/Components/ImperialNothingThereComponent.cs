using Robust.Shared.Map;
using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Polymorph;
namespace Content.Server.Imperial.SCP.NothingThere.Components;

[RegisterComponent]
public sealed partial class ImperialNothingThereComponent : Component
{
    [DataField("chaseSound")]
    public SoundSpecifier ChaseSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_idle.ogg");

    [ViewVariables]
    public EntityUid? PlayingStream;

    [ViewVariables]
    public bool IsPlaying = false;
    [DataField("enterBodyAction")]
    public EntProtoId EnterBodyAction = "ImperialNothingThereEnterBodyAction";
    [AutoNetworkedField, DataField("enterBodyEntity")]
    public EntityUid? EnterBodyEntity;

    [DataField, AutoNetworkedField]
    public TimeSpan EnterBodyWindUp = TimeSpan.FromSeconds(2);
    [DataField("threshold", required: true)]
    public FixedPoint2 Threshold = new();
    [DataField("killsRequired")]
    public int KillsRequired = 5;

    [DataField("killCount")]
    public int KillCount = 0;

    [DataField("phase")]
    public int Phase = 0;
    public EntProtoId? EggEntityProto = "ImperialSCPNothingThereEgg";
    [DataField("transformEggAction")]
    public EntProtoId TransformEggAction = "ImperialNothingThereEggAction";
    [AutoNetworkedField, DataField("transformEggEntity")]
    public EntityUid? TransformEggEntity;
    [DataField("enterSound")]
    public SoundSpecifier EnterSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_enter.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EggTransformDuration = TimeSpan.FromSeconds(30);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EggTransformEnd = TimeSpan.Zero;
    public EntProtoId? TrueEntityProto = "ImperialSCPNothingThereTrue";
    [DataField("hatchSound")]
    public SoundSpecifier HatchSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_hatch.ogg");

    public bool Empowered = false;
    [DataField("empowerAction")]
    public EntProtoId EmpowerAction = "ImperialNothingThereHelloAction";
    [AutoNetworkedField, DataField("empowerEntity")]
    public EntityUid? EmpowerEntity;
    [DataField("empowerSound")]
    public SoundSpecifier EmpowerSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_empower.ogg");
    [DataField(required: true), ViewVariables]
    public DamageSpecifier EmpowerDamage = new();
    [ViewVariables]
    public ProtoId<PolymorphPrototype> EggMorph = "NothingThereEggMorph";
    [ViewVariables]
    public ProtoId<PolymorphPrototype> TrueMorph = "NothingThereTrueMorph";
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GoodbyeDelay = TimeSpan.FromSeconds(0.5f);
    public EntProtoId GoodbyeProto = "ImperialNothingThereEmpowerHit";
    public EntProtoId HitProto = "ImperialNothingThereHit";
    [DataField]
    public bool NeedItems = false;
    public bool NeedGoodbye = false;
    [DataField("projectileAction")]
    public EntProtoId ProjectileAction = "ImperialNothingThereProjectileAction";
    [AutoNetworkedField, DataField("projectileEntity")]
    public EntityUid? ProjectileEntity;
}
