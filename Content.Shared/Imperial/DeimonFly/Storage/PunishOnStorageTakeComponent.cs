using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.DeimonFly.Storage;

/// <summary>
/// Наказывает игрока, если он вытаскивает указанные предметы из хранилища.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PunishOnStorageTakeComponent : Component
{
    [DataField] public List<ProtoId<EntityPrototype>> TargetItems = new();
    [DataField] public DamageSpecifier Damage = new();
    [DataField] public SoundSpecifier? Sound;
    [DataField] public string? Popup;
    [DataField] public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);
    [DataField] public TimeSpan LastPunish;
}
