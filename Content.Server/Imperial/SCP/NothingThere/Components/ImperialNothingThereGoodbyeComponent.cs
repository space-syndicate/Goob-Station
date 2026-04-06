using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
[RegisterComponent]
public sealed partial class ImperialNothingThereGoodbyeComponent : Component
{
    [DataField]
    public bool Used = false;
    public EntProtoId HitProto = "ImperialNothingThereHit";
    [DataField("empowerSound")]
    public SoundSpecifier EmpowerSound = new SoundPathSpecifier("/Audio/Imperial/SCP/nothingthere_empower.ogg");
}
