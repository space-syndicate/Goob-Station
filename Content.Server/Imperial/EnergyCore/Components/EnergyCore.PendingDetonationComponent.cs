using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore;
/// <summary>
/// Система энергетического ядра.
/// </summary>
namespace Content.Server.Imperial.EnergyCore.Components;

[RegisterComponent]
[Access(typeof(EnergyCorePendingDetonationSystem))]
public sealed partial class EnergyCorePendingDetonationComponent : Component
{
    // Время до детонации ядра при расплавлении. Этот параметр перезаписывается цваркой
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DetonationTime = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MusicTime = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool PlayedMusic = false;

    // Музыка детонации.
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier MeltdownMusic = new SoundPathSpecifier("/Audio/Imperial/level.ogg"); // Старый добрый level

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier BackgroundSiren = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/background_siren.ogg");

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier CoreAmbience2 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_2.ogg");

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier CoreAmbience3 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_3.ogg");
}
