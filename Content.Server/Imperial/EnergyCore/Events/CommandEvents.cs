using Robust.Shared.GameObjects;

namespace Content.Server.Imperial.EnergyCore.Events
{
    // Команды
    public sealed class Corearm : EntityEventArgs
    {}
    public sealed class CoreRecovery : EntityEventArgs
    {
        public bool Announce;
    }
}
