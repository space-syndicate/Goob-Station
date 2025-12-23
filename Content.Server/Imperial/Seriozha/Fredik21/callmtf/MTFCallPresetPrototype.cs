using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Imperial.MTFCall;

[Serializable, Prototype("MTFCall")]
public sealed class MTFCallPresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("path")] public string Path { get; set; } = string.Empty;

    [DataField("desc")] public string Desc { get; set; } = string.Empty;

    [DataField("musicPath")] public string? MusicPath { get; set; }

    [DataField("announcementMessage")] public string? AnnouncementMessage { get; set; }

    // Новое поле для отправителя
    [DataField("announcementSender")] public string? AnnouncementSender { get; set; }

    [DataField("musicParams")] public AudioParams MusicParams { get; set; } = AudioParams.Default.WithVolume(0f);
}
