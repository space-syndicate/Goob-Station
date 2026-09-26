using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }
    public NetEntity? SourceUid { get; }
    public bool IsWhisper { get; }
    public bool IsRadio { get; }
    public float? Pitch { get; }

    public PlayTTSEvent(byte[] data, NetEntity? sourceUid = null,
        bool isWhisper = false, bool isRadio = false, float? pitch = null)
    {
        Data = data;
        SourceUid = sourceUid;
        IsWhisper = isWhisper;
        IsRadio = isRadio;
        Pitch = pitch;
    }
}
