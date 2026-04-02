using Content.Shared.Zombies;
using Content.Shared.Damage.Systems;
using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Imperial.Zombies
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
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", FixedPoint2.New(0.20));

            var hands = EnsureComp<HandsComponent>(ent);
            if (hands.Hands.Count == 0)
            {
                _hands.AddHand(ent.Owner, "left", HandLocation.Left);
                _hands.AddHand(ent.Owner, "right", HandLocation.Right);
            }
            EnsureComp<PullerComponent>(ent);
            EnsureComp<BarotraumaComponent>(ent).Damage = damage;

            RemCompDeferred<ComplexInteractionComponent>(ent);
        }
    }
}
