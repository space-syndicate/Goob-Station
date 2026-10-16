using System.Linq;
using Content.Goobstation.Shared._Aquila.Xenobiology.Components;
using Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;
using Content.Goobstation.Shared._Aquila.Xenobiology.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Effects;

public sealed partial class AddSlimeTraitEffectSystem : EntityEffectSystem<SlimeGenesComponent, AddSlimeTrait>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SlimeGeneticsSystem _genetics = default!;

    protected override void Effect(Entity<SlimeGenesComponent> entity, ref EntityEffectEvent<AddSlimeTrait> args)
    {
        if (args.Effect.Trait is { } trait)
        {
            _genetics.TryAddTrait(entity.AsNullable(), trait);
            return;
        }

        var traits = _proto.EnumeratePrototypes<SlimeTraitPrototype>().ToList();
        _random.Shuffle(traits);

        foreach (var candidate in traits)
        {
            if (_genetics.TryAddTrait(entity.AsNullable(), candidate.ID))
                return;
        }
    }
}
