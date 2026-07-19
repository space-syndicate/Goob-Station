using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

/// <summary>
/// Компонент картриджа НаноЧата, вставляемого в КПК.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class NanoChatCartridgeComponent : Component
{
    /// <summary>
    /// ID пользователя (картриджа) в системе чата
    /// </summary>
    [DataField]
    public NetEntity? UserId;

    /// <summary>
    /// Имя из КПК
    /// </summary>
    [DataField]
    public string? PdaCardName;

    /// <summary>
    /// Выбранный в данный момент чат
    /// </summary>
    [DataField]
    public int? SelectedChat;

    /// <summary>
    /// Включены ли уведомления
    /// </summary>
    [DataField]
    public bool NotificationsOn = true;

    /// <summary>
    /// Виден ли этот пользователь другим, устанавливается только при спавне картриджа
    /// </summary>
    [DataField]
    public bool Visible = true;

    /// <summary>
    /// Звук уведомления
    /// </summary>
    [DataField]
    public SoundSpecifier NotificationSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");

    /// <summary>
    /// К какому серверу подключен клиент
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectedServer;

    /// <summary>
    /// Непрочитанные сообщения
    /// </summary>
    [DataField]
    public Dictionary<int, int> UnreadMessages = new();

    /// <summary>
    /// Когда можно ещё раз распечатать
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextPrintAllowedAfter = TimeSpan.Zero;

    /// <summary>
    /// Период между разрешёнными печатями
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Прототип бумаги для печати
    /// </summary>
    [DataField]
    public EntProtoId PaperId = "PaperNanoChatItem";

    /// <summary>
    /// Прототип штампа для печати
    /// </summary>
    [DataField]
    public EntProtoId StampId = "RubberStampNanoChat";

    /// <summary>
    /// Звук уведомления
    /// </summary>
    [DataField]
    public SoundSpecifier PrintingSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
