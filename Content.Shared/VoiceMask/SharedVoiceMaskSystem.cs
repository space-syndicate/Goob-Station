// SPDX-License-Identifier: MIT

using Content.Shared.StatusIcon; // GabyStation radio icons
using Robust.Shared.Prototypes; // GabyStation radio icons
using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public enum VoiceMaskUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VoiceMaskBuiState : BoundUserInterfaceState
{
    public readonly string Name;
    public readonly string? Verb;
    public readonly bool Active;
    public readonly bool AccentHide;
    public readonly string TTSVoice; // CorvaxGoob-TTS
    public ProtoId<JobIconPrototype>? JobIcon { get; } // GabyStation -> Radio icons

    public VoiceMaskBuiState(string name, string? verb, bool active, bool accentHide,
        ProtoId<JobIconPrototype>? jobIcon, string voice) // GabyStation radio icons) // CorvaxGoob-TTS
    {
        Name = name;
        Verb = verb;
        Active = active;
        AccentHide = accentHide;
        TTSVoice = voice;  // CorvaxGoob-TTS
        JobIcon = jobIcon; // GabyStation -> Radio icons
    }
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public VoiceMaskChangeNameMessage(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Change the speech verb prototype to override, or null to use the user's verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVerbMessage : BoundUserInterfaceMessage
{
    public readonly string? Verb;

    public VoiceMaskChangeVerbMessage(string? verb)
    {
        Verb = verb;
    }
}

/// <summary>
///     Toggle the effects of the voice mask.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskToggleMessage : BoundUserInterfaceMessage;

/// <summary>
///     Toggle the effects of accent negation.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskAccentToggleMessage : BoundUserInterfaceMessage;
