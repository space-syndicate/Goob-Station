using Robust.Shared.GameObjects;

namespace Content.Server.Imperial.EnergyCore.Events
{
    // События ядра
    public sealed class CoreCompromisedEvent : EntityEventArgs
    {}
    public sealed class CoreDetonatedEvent : EntityEventArgs
    {
        public EntityUid? OwningStation;
    }
}
