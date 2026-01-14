using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Server.Imperial.SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106BedComponent : Component
{
    [DataField("teleportSound")]
    public SoundSpecifier ContainmentSound = new SoundPathSpecifier("/Audio/Imperial/Seriozha/SCP/event/recontaime106.ogg");
    [DataField("globalTeleportSound")]
    public SoundSpecifier GlobalTeleportSound = new SoundPathSpecifier("/Audio/Imperial/SCP/scp106_teleported_global.ogg");
    [DataField]
    public ProtoId<EmotePrototype> PrototypeScream = "Scream";
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();
    [DataField("triggerPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TriggerPort = "Trigger";
    public EntityUid Victim = EntityUid.Invalid;
    [DataField]
    public float StaminaDamage = 100f;
    [ViewVariables]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DelayDamage { get; set; } = TimeSpan.FromSeconds(3.7);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DamageEnd = TimeSpan.Zero;
    public bool Started = false;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DelayContainment { get; set; } = TimeSpan.FromSeconds(31);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ContainmentEnd = TimeSpan.Zero;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(300);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CooldownEnd = TimeSpan.Zero;

}
