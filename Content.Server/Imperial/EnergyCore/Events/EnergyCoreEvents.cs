using Robust.Shared.GameObjects;

namespace Content.Server.Imperial.EnergyCore
{
    public sealed class CoreCompromisedEvent : EntityEventArgs
    {
    }
    public sealed class CoreDetonatedEvent : EntityEventArgs
    {
        public EntityUid? OwningStation;
    }
}
