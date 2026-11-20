// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Zekins <zekins3366@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Client._BlackM.SpeechBarks;
using Content.Shared._BlackM.CCVar;
using Content.Shared._BlackM.SpeechBarks;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<BlackMBarkPrototype> _barkList = new();
    private bool _barkUpdating;

    private static readonly BarkMode[] BarkModes = { BarkMode.Syllables, BarkMode.Letters, BarkMode.Words };

    private void InitializeBarks()
    {
        if (!_cfgManager.GetCVar(BlackMCVars.BarksEnabled))
        {
            BarksSection.Visible = false;
            return;
        }

        BarksSection.Visible = true;
        _barkList = SpeechBarksSystem.GetSelectableBarks(_prototypeManager);
        BarkSelectButton.Clear();
        for (var i = 0; i < _barkList.Count; i++)
        {
            BarkSelectButton.AddItem(GetBarkName(_barkList[i].ID), i);
        }

        BarkPitchSlider.MinValue = _cfgManager.GetCVar(BlackMCVars.BarksMinPitch);
        BarkPitchSlider.MaxValue = Math.Max(BarkPitchSlider.MinValue, _cfgManager.GetCVar(BlackMCVars.BarksMaxPitch));
        BarkVariationSlider.MinValue = BarkData.MinVariation;
        BarkVariationSlider.MaxValue = BarkData.MaxVariation;
        BarkSpeedSlider.MinValue = BarkData.MinSpeed;
        BarkSpeedSlider.MaxValue = BarkData.MaxSpeed;

        BarkModeButton.Clear();
        for (var i = 0; i < BarkModes.Length; i++)
        {
            BarkModeButton.AddItem(Loc.GetString($"blackm-bark-mode-{BarkModes[i].ToString().ToLowerInvariant()}"), i);
        }

        BarkSelectButton.OnItemSelected += args =>
        {
            BarkSelectButton.SelectId(args.Id);
            if (args.Id >= 0 && args.Id < _barkList.Count)
                SetBarkProto(_barkList[args.Id].ID);
        };
        BarkVoicePlayButton.OnPressed += _ => PlayBarkPreview();
        BarkAdvancedButton.OnToggled += args => BarkAdvancedContainer.Visible = args.Pressed;
        BarkResetButton.OnPressed += _ => ResetBark();
        BarkPreviewEdit.OnTextEntered += _ => PlayBarkPreview();

        BarkPitchSlider.OnValueChanged += _ => UpdateBark(b => b.WithPitch(MathF.Round(BarkPitchSlider.Value, 2)));
        BarkVariationSlider.OnValueChanged += _ => UpdateBark(b => b.WithVariation(MathF.Round(BarkVariationSlider.Value, 2)));
        BarkSpeedSlider.OnValueChanged += _ => UpdateBark(b => b.WithSpeed(MathF.Round(BarkSpeedSlider.Value, 2)));

        BarkModeButton.OnItemSelected += args =>
        {
            BarkModeButton.SelectId(args.Id);
            UpdateBark(b => b.WithMode(BarkModes[args.Id]));
        };
    }

    private void UpdateBarkVoiceControls()
    {
        if (Profile is null || !BarksSection.Visible)
            return;

        _barkUpdating = true;
        try
        {
            var bark = Profile.Bark;

            var barkIndex = _barkList.FindIndex(b => b.ID == bark.Proto);
            if (barkIndex >= 0)
                BarkSelectButton.SelectId(barkIndex);
            BarkPitchSlider.Value = bark.Pitch;
            BarkVariationSlider.Value = bark.Variation;
            BarkSpeedSlider.Value = bark.Speed;
            BarkModeButton.SelectId(Math.Max(0, Array.IndexOf(BarkModes, bark.Mode)));

            UpdateBarkLabels(bark);
        }
        finally
        {
            _barkUpdating = false;
        }
    }

    private void UpdateBarkLabels(BarkData bark)
    {
        BarkPitchLabel.Text = bark.Pitch.ToString("F2");
        BarkVariationLabel.Text = $"±{bark.Variation * 100:F0}%";
        BarkSpeedLabel.Text = $"x{bark.Speed:F2}";
    }

    private void UpdateBark(Func<BarkData, BarkData> change)
    {
        if (_barkUpdating || Profile is null)
            return;

        var bark = change(Profile.Bark);
        if (bark.Equals(Profile.Bark))
            return;

        Profile = Profile.WithBark(bark);
        UpdateBarkLabels(bark);
        IsDirty = true;
    }

    private string GetBarkName(string protoId)
    {
        return _prototypeManager.TryIndex<BlackMBarkPrototype>(protoId, out var proto)
            ? Loc.GetString(proto.Name)
            : protoId;
    }

    private void SetBarkProto(string barkId)
    {
        if (Profile is null)
            return;

        UpdateBark(b => b.WithProto(barkId));
        var barkIndex = _barkList.FindIndex(b => b.ID == barkId);
        if (barkIndex >= 0)
            BarkSelectButton.SelectId(barkIndex);
    }

    private string GetBarkPreviewText()
    {
        var text = BarkPreviewEdit.Text.Trim();
        return string.IsNullOrEmpty(text) ? Loc.GetString("blackm-bark-preview-default") : text;
    }

    private void PlayBarkPreview()
    {
        if (Profile is null)
            return;

        PlayBarkPreview(Profile.Bark);
    }

    private void PlayBarkPreview(string protoId)
    {
        if (Profile is null)
            return;

        PlayBarkPreview(Profile.Bark.WithProto(protoId));
    }

    private void PlayBarkPreview(BarkData data)
    {
        var text = GetBarkPreviewText();
        BarkPreviewLabel.Text = string.Empty;

        _entManager.System<SpeechBarksSystem>().PlayPreview(
            data,
            text,
            count => BarkPreviewLabel.Text = text[..Math.Clamp(count, 0, text.Length)]);
    }

    private void ResetBark()
    {
        if (Profile is null)
            return;

        var proto = Profile.Bark.Proto;
        UpdateBark(_ => new BarkData(proto, BarkData.DefaultPitch, BarkData.DefaultVariation, BarkData.DefaultSpeed, BarkData.DefaultMode));
        UpdateBarkVoiceControls();
    }
}
