using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage.Components;
using Robust.Shared.Random;

namespace Content.Server.Imperial.Other.TFG.Toughguys;

/// <summary>
/// При инициализации сущности выбирает плюшки без повторения персонажей
/// и помещает их непосредственно в EntityStorage этой сущности.
/// </summary>
public sealed class UniqueRandomPlushieFillSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UniqueRandomPlushieFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<UniqueRandomPlushieFillComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<EntityStorageComponent>(ent, out var storage))
        {
            Log.Error($"{ToPrettyString(ent)} has UniqueRandomPlushieFill but no EntityStorage component.");
            return;
        }

        // Работаем с временными копиями: исходные списки компонента остаются неизменными
        // и могут повторно использоваться другими созданными ящиками.
        var rare = ent.Comp.RarePrototypes.Distinct().ToList();
        var common = ent.Comp.Prototypes.Distinct().ToList();

        // Не пытаемся создать больше сущностей, чем физически помещается в хранилище.
        var availableSlots = Math.Max(0, storage.Capacity - storage.Contents.ContainedEntities.Count);
        var attempts = Math.Min(Math.Max(0, ent.Comp.Amount), availableSlots);

        // Защита от ошибочных значений вероятности в YAML.
        var rareChance = Math.Clamp(ent.Comp.RareChance, 0f, 1f);
        var commonChance = Math.Clamp(ent.Comp.Chance, 0f, 1f);

        for (var i = 0; i < attempts && (rare.Count > 0 || common.Count > 0); i++)
        {
            string? prototype = null;

            if (rare.Count > 0 && _random.Prob(rareChance))
            {
                prototype = _random.PickAndTake(rare);
            }
            else if (common.Count > 0 && _random.Prob(commonChance))
            {
                prototype = _random.PickAndTake(common);
            }

            if (prototype == null)
                continue;

            // Gift-прототип является вариантом обычной игрушки, а не отдельным персонажем.
            // Удаляем всё семейство, чтобы, например, PlushieCP и PlushieCPGift
            // не могли одновременно оказаться в одном ящике.
            var family = GetPlushieFamily(prototype);
            rare.RemoveAll(candidate => GetPlushieFamily(candidate.Id) == family);
            common.RemoveAll(candidate => GetPlushieFamily(candidate.Id) == family);

            var spawned = Spawn(prototype, Transform(ent).Coordinates);
            if (_entityStorage.Insert(spawned, ent, storage))
                continue;

            Log.Error($"Failed to insert unique plushie {prototype} into {ToPrettyString(ent)}.");
            QueueDel(spawned);
        }
    }

    private static string GetPlushieFamily(string prototype)
    {
        // Все редкие варианты в текущем пуле используют единый суффикс Gift.
        // После его удаления ID совпадает с ID обычной версии персонажа.
        const string giftSuffix = "Gift";
        return prototype.EndsWith(giftSuffix, StringComparison.Ordinal)
            ? prototype[..^giftSuffix.Length]
            : prototype;
    }
}
