using Robust.Shared.GameObjects;

namespace Content.Server.Imperial.EnergyCore.Events
// События ядра
{
    public sealed class CoreCompromisedEvent : EntityEventArgs
    {
    }
    public sealed class CoreDetonatedEvent : EntityEventArgs
    {
        public EntityUid? OwningStation;
    }
// Команды
    public sealed class Corearm : EntityEventArgs
    {
    }
    public sealed class CoreRecovery : EntityEventArgs
    {
        public bool Announce;
    }
// Инициализация новых ядер или терминалов
    public sealed class CoreInitEvent : EntityEventArgs
    {}
    public sealed class CoreTerminalInitEvent : EntityEventArgs
    {}
}
