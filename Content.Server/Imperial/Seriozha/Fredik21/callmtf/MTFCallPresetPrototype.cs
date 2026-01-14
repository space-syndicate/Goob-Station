using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Imperial.MTFCall;

[Prototype("MTFCall")]
public sealed class MTFCallPresetPrototype : IPrototype
{
    [IdDataField] 
    public string ID { get; } = default!;

    /// <summary>
    /// Путь к файлу грида (карты) ОБР.
    /// </summary>
    [DataField("path", required: true)] 
    public ResPath Path { get; set; } = default!;

    /// <summary>
    /// Описание пресета в меню (Локализация).
    /// </summary>
    [DataField("desc")] 
    public LocId Desc { get; set; } = string.Empty;

    /// <summary>
    /// Звук анонса при прибытии.
    /// </summary>
    [DataField("announcementSound")] 
    public SoundSpecifier? AnnouncementSound { get; set; }

    /// <summary>
    /// Текст сообщения анонса (Локализация).
    /// </summary>
    [DataField("announcementMessage")] 
    public LocId? AnnouncementMessage { get; set; }

    /// <summary>
    /// Имя отправителя анонса (Локализация).
    /// </summary>
    [DataField("announcementSender")] 
    public LocId? AnnouncementSender { get; set; }
}
