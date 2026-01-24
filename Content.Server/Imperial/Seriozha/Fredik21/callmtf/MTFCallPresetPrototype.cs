using Robust.Shared.Prototypes; // Для IPrototype и [Prototype]
using Robust.Shared.Serialization.Manager.Attributes; // Для [DataField] и [IdDataField]
using Robust.Shared.Utility; // Для ResPath
using Robust.Shared.Audio; // Для SoundSpecifier
using Robust.Shared.Localization; // Для LocId

namespace Content.Server.Imperial.MTFCall;

[Prototype("MTFCall")]
public sealed class MTFCallPresetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Путь к файлу грида (карты) ОБР.
    /// </summary>
    [DataField(required: true)]
    public ResPath Path { get; private set; } = default!;

    /// <summary>
    /// Описание пресета в меню (Локализация).
    /// </summary>
    [DataField]
    public LocId Desc { get; private set; } = string.Empty;

    /// <summary>
    /// Звук анонса при прибытии.
    /// </summary>
    [DataField]
    public SoundSpecifier? AnnouncementSound { get; private set; }

    /// <summary>
    /// Текст сообщения анонса (Локализация).
    /// </summary>
    [DataField]
    public LocId? AnnouncementMessage { get; private set; }

    /// <summary>
    /// Имя отправителя анонса (Локализация).
    /// </summary>
    [DataField]
    public LocId? AnnouncementSender { get; private set; }
}
