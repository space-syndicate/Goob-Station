using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility; // Нужно для ResPath

namespace Content.Server.Imperial.MTFCall;

[Prototype("MTFCall")]
public sealed class MTFCallPresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    // Путь к гриду (карте) через ResPath
    [DataField("path")] public ResPath Path { get; set; } = default!;

    // Описание через ID локализации
    [DataField("desc")] public string Desc { get; set; } = string.Empty;

    // Звук через SoundSpecifier (поддерживает и пути, и коллекции)
    [DataField("announcementSound")] public SoundSpecifier? AnnouncementSound { get; set; }

    // Сообщение через ID локализации
    [DataField("announcementMessage")] public string? AnnouncementMessage { get; set; }

    // Отправитель через ID локализации
    [DataField("announcementSender")] public string? AnnouncementSender { get; set; }
}
