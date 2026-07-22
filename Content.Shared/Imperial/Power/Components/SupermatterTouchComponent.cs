using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterTouchComponent : Component
{
    /// <summary>
    /// Цвет вспышки при гибе
    /// </summary>
    [DataField]
    public Color FlashColor = new(1f, 0f, 0f, 0.8f);

    /// <summary>
    /// Звук гиба
    /// </summary>
    [DataField]
    public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    /// <summary>
    /// Прототип, используемый для праха после гиба.
    /// </summary>
    [DataField]
    public EntProtoId AshPrototype = "Ash";
}
