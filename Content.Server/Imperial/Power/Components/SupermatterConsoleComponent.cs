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
    /// Максимальный радиус подключения к суперматерии
    /// </summary>
    [DataField]
    public float MaxRange = 15f;

    /// <summary>
    /// Имя порта для связи
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// Звук пиликанья при низкой целостности
    /// </summary>
    [DataField]
    public SoundPathSpecifier BeepSound = new("/Audio/Machines/beep.ogg");

    /// <summary>
    /// Время следующего пиликанья
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextBeep = TimeSpan.Zero;

    /// <summary>
    /// Интервал пиликанья
    /// </summary>
    [DataField]
    public TimeSpan BeepInterval = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// Время следующего обновления UI
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextUiUpdate = TimeSpan.Zero;

    /// <summary>
    /// Интервал обновления UI
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.5f);
}
