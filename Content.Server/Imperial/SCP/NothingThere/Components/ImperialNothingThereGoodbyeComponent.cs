using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
[RegisterComponent]
public sealed partial class ImperialNothingThereGoodbyeComponent : Component
{
    [ViewVariables]
    public bool Used = false;
    [ViewVariables]
    public EntProtoId HitProto = "ImperialNothingThereHit";
    [DataField("empowerSound")]
    public SoundSpecifier EmpowerSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_empower.ogg");
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AttackDuration = TimeSpan.FromSeconds(1.5);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan AttackEnd = TimeSpan.Zero;
    [ViewVariables]
    public EntityUid WeaponUser = EntityUid.Invalid;
}
