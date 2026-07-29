// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Audio;
using Content.Shared._CorvaxGoob.CCCVars;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class AudioTab
{
    /// <summary>
    /// Registers client options that control headset radio receive sounds.
    /// </summary>
    private void InitializeRadioSoundOptions()
    {
        Control.AddOptionPercentSlider(
            CCCVars.RadioVolume,
            SliderVolumeRadioSound,
            scale: ContentAudioSystem.RadioMultiplier);

        SliderRadioSoundCooldown.Slider.Rounded = true;
        Control.AddOption(new OptionSliderFloatCVar(
            Control,
            _cfg,
            CCCVars.RadioSoundCooldown,
            SliderRadioSoundCooldown,
            1f,
            10f,
            1f,
            FormatRadioSoundCooldown));
    }

    private static string FormatRadioSoundCooldown(OptionSliderFloatCVar slider, float value)
    {
        return Loc.GetString("ui-options-radio-sound-value-seconds", ("value", (int) value));
    }
}
