using Content.Shared.Damage;

namespace Content.Shared.Imperial.Damage.Events;

public sealed class ImperialShieldRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public ImperialShieldRelayedEvent(TEvent args)
    {
        Args = args;
    }
}
