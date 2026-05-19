using Robust.Shared.Audio;

namespace Content.Server.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterMonitorConsoleComponent : Component
{
    /// <summary>
    /// Таймер до следующего пиликанья
    /// </summary>
    public TimeSpan BeepCooldownTimer = TimeSpan.Zero;

    /// <summary>
    /// Звук пиликанья при низкой целостности
    /// </summary>
    [DataField]
    public SoundPathSpecifier BeepSound = new("/Audio/Machines/beep.ogg");
}
