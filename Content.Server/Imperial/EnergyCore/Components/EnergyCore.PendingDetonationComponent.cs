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
    // Время до детонации ядра при расплавлении
    [DataField]
    public TimeSpan DetonationTime = TimeSpan.FromSeconds(300f); // 5 минут

    [DataField]
    public TimeSpan MusicTime = TimeSpan.Zero;

    [DataField]
    public bool PlayedMusic = false;

    // Музыка детонации.
    [DataField]
    public SoundSpecifier MeltdownMusic = new SoundPathSpecifier("/Audio/Imperial/level.ogg"); // Я хрен знает под какой лицензией распространяется Trumped Up Charges

    [DataField]
    public SoundSpecifier BackgroundSiren = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/background_siren.ogg");

    [DataField]
    public SoundSpecifier CoreAmbience2 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_2.ogg");

    [DataField]
    public SoundSpecifier CoreAmbience3 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_3.ogg");
}
