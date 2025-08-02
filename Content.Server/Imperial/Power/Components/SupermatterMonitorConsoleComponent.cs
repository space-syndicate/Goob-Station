using Robust.Shared.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.Imperial.Power.Components
{
    [RegisterComponent]
    public sealed partial class SupermatterMonitorConsoleComponent : Component
    {
        // Таймер до следующего пиликанья
        [DataField]
        public TimeSpan BeepCooldownTimer = TimeSpan.Zero;
        // Звук пиликанья при низкой целостности
        [DataField]
        public SoundPathSpecifier BeepSound = new("/Audio/Machines/beep.ogg");
    }
}
