using Content.Shared.Zombies;
using Content.Server.Atmos.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;

namespace Content.Server.Zombies
{
    public sealed partial class ZombifySystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _hands = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PendingZombieComponent, EntityZombifiedEvent>(OnZombified);
        }

        private void OnZombified(Entity<PendingZombieComponent> ent, ref EntityZombifiedEvent args)
        {
            if (!TryComp<ZombieBarotraumaDamageComponent>(ent, out var zombieBaro))
                return;

            var hands = EnsureComp<HandsComponent>(ent);
            if (hands.Hands.Count == 0)
            {
                _hands.AddHand(ent.Owner, "left", HandLocation.Left);
                _hands.AddHand(ent.Owner, "right", HandLocation.Right);
            }
            EnsureComp<PullerComponent>(ent);
            EnsureComp<BarotraumaComponent>(ent).Damage = zombieBaro.Damage;

            RemCompDeferred<ZombieBarotraumaDamageComponent>(ent);
            RemCompDeferred<ComplexInteractionComponent>(ent);
        }
    }
}
