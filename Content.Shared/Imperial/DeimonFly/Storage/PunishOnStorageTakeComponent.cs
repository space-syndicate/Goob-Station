using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.DeimonFly.Storage;

/// <summary>
/// Компонент, который наказывает игрока при изъятии определённых предметов из хранилища.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PunishOnStorageTakeComponent : Component
{
    /// <summary>
    /// Прототипы предметов, забирая которые игрок получит наказание.
    /// </summary>
    [DataField] public List<ProtoId<EntityPrototype>> TargetItems = new();

    /// <summary>
    /// Урон, который будет нанесён игроку.
    /// </summary>
    [DataField] public DamageSpecifier Damage = new();

    /// <summary>
    /// Звук, проигрываемый при наказании.
    /// </summary>
    [DataField] public SoundSpecifier? Sound;

    /// <summary>
    /// Ключ локализации попапа, показанного жертве.
    /// </summary>
    [DataField] public string? Popup;

    /// <summary>
    /// Кулдаун между срабатываниями.
    /// </summary>
    [DataField] public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Время последнего срабатывания (для кулдауна).
    /// </summary>
    [DataField] public TimeSpan LastPunish;
}
