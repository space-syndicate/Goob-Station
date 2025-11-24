using Content.Shared.Actions;

namespace Content.Shared.Imperial.Vampire
{
    public sealed class VampireEnvelopeGhoulEvent : EntityEventArgs
    {
        public EntityUid Vampire { get; }
        public EntityUid Target { get; }

        public VampireEnvelopeGhoulEvent(EntityUid vampire, EntityUid target)
        {
            Vampire = vampire;
            Target = target;
        }
    }
}
