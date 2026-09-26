using Content.Shared._CorvaxGoob.TTS;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Effects;

namespace Content.Client._CorvaxGoob.TTS;

public sealed partial class TTSSystem
{
    // TODO-Corvax: Если губы и мы обновимся до версии движка 283.0.0 посмотреть как этот файл устроен на ванилле и сделать также. 
    private EntityUid? _radioAuxiliaryEntity;
    private EntityUid? _voiceAuxiliaryEntity;
    private EntityUid? _cachedVoiceEffectEntity;
    private EntityUid? _cachedRadioEffectEntity;
    private readonly object _voiceEffectLock = new();
    private readonly object _radioEffectLock = new();

    /// <summary>
    /// It frees up only the vocal effect and its auxiliary.
    /// </summary>
    private void ShutdownVoiceEffect()
    {
        UnlinkAuxiliary(_voiceAuxiliaryEntity);

        if (_cachedVoiceEffectEntity != null && !TerminatingOrDeleted(_cachedVoiceEffectEntity.Value))
        {
            _audio.Stop(_cachedVoiceEffectEntity);
            Del(_cachedVoiceEffectEntity);
        }
        _cachedVoiceEffectEntity = null;
    }

    /// <summary>
    /// It frees up only the radio effect and its auxiliary.
    /// </summary>
    private void ShutdownRadioEffect()
    {
        UnlinkAuxiliary(_radioAuxiliaryEntity);

        if (_cachedRadioEffectEntity != null && !TerminatingOrDeleted(_cachedRadioEffectEntity.Value))
        {
            _audio.Stop(_cachedRadioEffectEntity);
            Del(_cachedRadioEffectEntity);
        }
        _cachedRadioEffectEntity = null;
    }

    /// <summary>
    /// Clears all EFX-effects.
    /// </summary>
    private void ShutdownEffects()
    {
        ShutdownVoiceEffect();
        ShutdownRadioEffect();
    }

    private void UnlinkAuxiliary(EntityUid? auxiliary)
    {
        if (auxiliary != null && TryComp<AudioAuxiliaryComponent>(auxiliary.Value, out var auxComp))
            _audio.SetEffect(auxiliary.Value, auxComp, null);
    }

    private void ApplyVoiceEffect((EntityUid Entity, AudioComponent Component) audio, TTSVoiceEffectPreset effect)
    {
        if (!_ttsEnabled)
            return;

        if (effect == TTSVoiceEffectPreset.None)
            return;

        if (!EnsureVoiceEffectInitialized())
            return;

        if (_voiceAuxiliaryEntity == null)
            return;

        try
        {
            var (entity, comp) = audio;
            _audio.SetAuxiliary(entity, comp, _voiceAuxiliaryEntity.Value);
        }
        catch (Exception ex)
        {
            _sawmill.Debug($"Failed to apply voice effect: {ex.Message}");
        }
    }

    private void ApplyRadioEffect((EntityUid Entity, AudioComponent Component) audio)
    {
        if (!_ttsEnabled)
            return;

        if (!EnsureRadioEffectInitialized())
            return;

        if (_radioAuxiliaryEntity == null)
            return;

        try
        {
            var (entity, comp) = audio;
            _audio.SetAuxiliary(entity, comp, _radioAuxiliaryEntity.Value);
        }
        catch (Exception ex)
        {
            _sawmill.Debug($"Failed to apply radio EFX effect: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an effect and an auxiliary, configures the preset, and links them.
    /// Rolls back everything created and returns null if any error occurs.
    /// </summary>
    private EntityUid? TryCreateEffect(ReverbProperties preset, ref EntityUid? auxiliary, string debugName)
    {
        EntityUid? effectUid = null;

        try
        {
            var (effect, effectComp) = _audio.CreateEffect();
            effectUid = effect;

            _audio.SetEffectPreset(effect, effectComp, preset);

            if (auxiliary == null || TerminatingOrDeleted(auxiliary.Value))
                auxiliary = _audio.CreateAuxiliary().Entity;

            _audio.SetEffect(auxiliary.Value, Comp<AudioAuxiliaryComponent>(auxiliary.Value), effect);

            return effect;
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"Failed to initialize {debugName} effect: {ex.Message}");

            if (effectUid != null && !TerminatingOrDeleted(effectUid.Value))
            {
                _audio.Stop(effectUid.Value);
                Del(effectUid.Value);
            }

            return null;
        }
    }

    /// <summary>
    /// Initializes voice effect upon first use, if necessary.
    /// </summary>
    private bool EnsureVoiceEffectInitialized()
    {
        if (!_ttsEnabled)
            return false;

        if (_voiceEffectPreset == TTSVoiceEffectPreset.None)
        {
            _cachedVoiceEffectEntity = null;
            return false;
        }

        if (_cachedVoiceEffectEntity != null)
            return true;

        lock (_voiceEffectLock)
        {
            if (_cachedVoiceEffectEntity != null)
                return true;

            if (_voiceEffectPreset == TTSVoiceEffectPreset.None)
            {
                _cachedVoiceEffectEntity = null;
                return false;
            }

            var result = TryCreateEffect(GetVoicePreset(_voiceEffectPreset), ref _voiceAuxiliaryEntity, $"voice ({_voiceEffectPreset})");
            if (result == null)
                return false;

            _cachedVoiceEffectEntity = result;

            _sawmill.Info($"Voice effect initialized: {_voiceEffectPreset}");
            return true;
        }
    }

    /// <summary>
    /// Initializes radio effect on first use.
    /// </summary>
    private bool EnsureRadioEffectInitialized()
    {
        if (!_ttsEnabled)
            return false;

        if (_cachedRadioEffectEntity != null)
            return true;

        lock (_radioEffectLock)
        {
            if (_cachedRadioEffectEntity != null)
                return true;

            var result = TryCreateEffect(CreateRadioPreset(), ref _radioAuxiliaryEntity, "radio");
            if (result == null)
                return false;

            _cachedRadioEffectEntity = result;

            return true;
        }
    }
}
