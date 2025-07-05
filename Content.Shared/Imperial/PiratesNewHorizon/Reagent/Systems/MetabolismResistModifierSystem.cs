using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;
using Content.Shared.Damage.Events;
using Content.Shared.Damage;
using Content.Shared.Imperial.PiratesNewHorizon.Reagent.Components;
namespace Content.Shared.Imperial.PiratesNewHorizon.Reagent.Systems
{
    public sealed class MetabolismResistModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movespeed = default!;

        private readonly List<Entity<ResistModifierMetabolismComponent>> _components = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;
            SubscribeLocalEvent<ResistModifierMetabolismComponent, DamageModifyEvent>(OnDamageModify);
            SubscribeLocalEvent<ResistModifierMetabolismComponent, ComponentStartup>(AddComponent);
        }

        private void OnDamageModify(EntityUid uid, ResistModifierMetabolismComponent component, DamageModifyEvent args)
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.Modifiers);
        }

        private void AddComponent(Entity<ResistModifierMetabolismComponent> metabolism, ref ComponentStartup args)
        {
            _components.Add(metabolism);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var currentTime = _gameTiming.CurTime;

            for (var i = _components.Count - 1; i >= 0; i--)
            {
                var metabolism = _components[i];

                if (metabolism.Comp.Deleted)
                {
                    _components.RemoveAt(i);
                    continue;
                }

                if (metabolism.Comp.ModifierTimer > currentTime)
                    continue;

                _components.RemoveAt(i);
                EntityManager.RemoveComponent<ResistModifierMetabolismComponent>(metabolism);

                _movespeed.RefreshMovementSpeedModifiers(metabolism);
            }
        }
    }
}
