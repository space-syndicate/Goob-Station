using Content.Server.Chat.Systems;
using Content.Shared._CorvaxGoob.TTS;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.TTS;

public sealed partial class TTSSystem
{
    public void SendTTSAdminAnnouncement(string text, string voice, string announcementPath = ChatSystem.CentComAnnouncementSound)
    {
        if (!_isEnabled || text.Length > MaxMessageChars || string.IsNullOrEmpty(voice))
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var protoVoice))
            return;

        HandleAdminAnnouncement(text, protoVoice.Speaker, new SoundPathSpecifier(announcementPath));
    }

    private async void HandleAdminAnnouncement(string text, string speaker, SoundSpecifier announcementSound)
    {
        var soundData = await GenerateTTS(text, speaker);
        if (soundData is null)
            return;

        var delay = _audio.GetAudioLength(_audio.ResolveSound(announcementSound))
                    + TimeSpan.FromSeconds(AnnouncementDelay);

        Timer.Spawn(delay, () => RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.Broadcast(), recordReplay: false));
    }
}
