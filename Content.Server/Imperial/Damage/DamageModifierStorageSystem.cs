using Content.Shared.Imperial.Damage.Components;
using Content.Shared.Storage;
using Content.Shared.Stacks;
using Content.Shared.FixedPoint;
 using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Imperial.Damage
{
    public sealed class DamageModifierStorageSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageModifierStorageComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        }

        private void OnGetMeleeDamage(EntityUid uid, DamageModifierStorageComponent component, ref GetMeleeDamageEvent args)
        {
            if (!_entityManager.TryGetComponent(uid, out StorageComponent? storage))
                return;

            var container = storage.Container;
            if (container == null || container.ContainedEntities.Count == 0)
                return;

            var containedEntities = container.ContainedEntities;
            var entityCount = containedEntities.Count;

            var entityManager = _entityManager;
            var targetBase = component.TargetItemBaseId.ToLowerInvariant();
            FixedPoint2 totalCount = FixedPoint2.Zero;

            for (var i = 0; i < entityCount; i++)
            {
                var entity = containedEntities[i];
                var meta = entityManager.GetComponent<MetaDataComponent>(entity);
                var protoId = meta.EntityPrototype?.ID;

                if (string.IsNullOrEmpty(protoId))
                    continue;

                var protoIdLower = protoId.ToLowerInvariant();
                if (!protoIdLower.StartsWith(targetBase))
                    continue;

                if (entityManager.TryGetComponent(entity, out StackComponent? stack))
                    totalCount += (FixedPoint2)stack.Count;
                else
                    totalCount += (FixedPoint2)1;
            }

            if (totalCount == FixedPoint2.Zero)
                return;

            var newDamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>(args.Damage.DamageDict);
            var damageIncrease = component.DamageIncrease * totalCount;

            if (newDamageDict.TryGetValue("Blunt", out var currentBlunt))
                newDamageDict["Blunt"] = currentBlunt + damageIncrease;
            else
                newDamageDict["Blunt"] = damageIncrease;

            args.Damage = new Content.Shared.Damage.DamageSpecifier
            {
                DamageDict = newDamageDict
            };
        }
    }
}
