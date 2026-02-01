using Content.Server.Imperial.ImperialStore;
using Content.Server.Imperial.Medieval.Magic.BindStoreOnEquip;
using Content.Shared.EntityEffects;
using Content.Shared.Imperial.Medieval.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.EntityEffects;



public sealed partial class GAddMagicEssenceEffectSystem : EntityEffectSystem<MetaDataComponent, AddMagicEssence>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddMagicEssence> args)
    {

        var enumerator = _entityManager.EntityQueryEnumerator<BindStoreOnEquipComponent>();

        while (enumerator.MoveNext(out var spellBookUid, out var bindStoreOnEquipComponent))
        {
            if (bindStoreOnEquipComponent.BindedEntity != args.User) continue;

            foreach (var (currencyPrototype, count) in args.Effect.AddedEssences)
                TryAddEssence(currencyPrototype, count, spellBookUid);

            return;
        }
    }

    #region Helpers

    private bool TryAddEssence(EntProtoId currencyPrototype, int count, EntityUid spellBookUid)
    {
        var imperialStoreSystem = _entityManager.System<ImperialStoreSystem>();

        var addOneOrMoreEssence = false;

        for (var i = 0; i < count; i++)
        {
            var essenceEntity = _entityManager.Spawn(currencyPrototype);

            addOneOrMoreEssence = true;
            imperialStoreSystem.TryAddCurrency(essenceEntity, spellBookUid);
        }

        return addOneOrMoreEssence;
    }

    #endregion
}
