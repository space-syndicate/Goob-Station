using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterConsoleComponent : Component
{
    /// <summary>
    /// Подключённая суперматерия
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedSupermatter;

    /// <summary>
    /// Имя порта для связи
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// Таймер до следующего пиликанья
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BeepCooldownTimer = TimeSpan.Zero;
    [DataField]
    public TimeSpan BeepCooldown = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// Звук пиликанья при низкой целостности
    /// </summary>
    [DataField]
    public SoundPathSpecifier BeepSound = new("/Audio/Machines/beep.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UiUpdateTimer = TimeSpan.Zero;
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.5);
}
