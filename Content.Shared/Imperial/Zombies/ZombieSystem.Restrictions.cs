using Content.Shared.Zombies;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Imperial.Zombies
{
    public sealed partial class ZombifySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, PullAttemptEvent>(OnPullAttempt);

            SubscribeLocalEvent<ZombieComponent, PickupAttemptEvent>(OnAttemptPickup);
            SubscribeLocalEvent<ZombieComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        }

        private void OnPullAttempt(Entity<ZombieComponent> ent, ref PullAttemptEvent args)
        {
            if (args.PullerUid == ent.Owner)
            {
                if (!HasComp<ZombieComponent>(args.PulledUid))
                {
                    args.Cancelled = true;
                }
            }
        }

        private void OnAttemptPickup(Entity<ZombieComponent> ent, ref PickupAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnUnequipAttempt(Entity<ZombieComponent> ent, ref IsUnequippingAttemptEvent args)
        {
            args.Cancel();
        }
    }
}
