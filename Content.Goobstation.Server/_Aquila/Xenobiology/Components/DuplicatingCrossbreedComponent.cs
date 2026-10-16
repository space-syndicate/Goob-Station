using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, Access(typeof(DuplicatingCrossbreedSystem))]
public sealed partial class DuplicatingCrossbreedComponent : Component
{
    /// <summary>
    /// Сколько копий создаётся за одно применение.
    /// </summary>
    [DataField]
    public int Copies = 1;

    /// <summary>
    /// Сколько применений осталось до исчезновения гибрида.
    /// </summary>
    [DataField]
    public int Uses = 1;

    /// <summary>
    /// Сущности, которые можно скопировать.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Сущности, которые копировать нельзя.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Звук при копировании.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");

    /// <summary>
    /// Визуальный эффект на скопированной сущности.
    /// </summary>
    [DataField]
    public EntProtoId? Effect = "EffectXenobioDuplicate";
}
