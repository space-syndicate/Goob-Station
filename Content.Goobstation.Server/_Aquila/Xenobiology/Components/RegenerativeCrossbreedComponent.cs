using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, Access(typeof(RegenerativeCrossbreedSystem))]
public sealed partial class RegenerativeCrossbreedComponent : Component
{
    /// <summary>
    /// Эффекты, применяемые к цели.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Звук при применении.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    /// <summary>
    /// Визуальные эффекты, прикрепляемые к цели при применении.
    /// </summary>
    [DataField]
    public List<EntProtoId> VisualEffects = new() { "EffectXenobioRegenerativeRing", "EffectXenobioRegenerative" };
}
