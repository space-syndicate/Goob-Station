using System.Linq;
using Content.Goobstation.Shared._Aquila.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Shared.Nutrition;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Systems;

public sealed class SlimeGeneticsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeGenesComponent, SlimeScannerInfoEvent>(OnSlimeScannerInfo);
        SubscribeLocalEvent<SlimeGenesComponent, IngestingEvent>(OnIngesting);
    }

    private void OnSlimeScannerInfo(Entity<SlimeGenesComponent> ent, ref SlimeScannerInfoEvent args)
    {
        if (TryComp<SlimeComponent>(ent, out var slime))
        {
            args.Text.AppendLine(Loc.GetString("slime-scanner-genes-offspring", ("num", GetMaxOffspring((ent, slime)))));
            args.Text.AppendLine(Loc.GetString("slime-scanner-genes-mitosis", ("hunger", MathF.Round(GetMitosisHunger((ent, slime))))));
        }

        if (ent.Comp.Traits.Count == 0)
        {
            args.Text.AppendLine(Loc.GetString("slime-scanner-genes-no-traits"));
            return;
        }

        args.Text.AppendLine(Loc.GetString("slime-scanner-genes-traits"));
        foreach (var trait in GetTraits(ent.Comp))
        {
            args.Text.AppendLine(Loc.GetString("slime-scanner-genes-trait",
                ("color", trait.Color.ToHex()),
                ("name", Loc.GetString(trait.Name)),
                ("description", Loc.GetString(trait.Description))));
        }
    }

    private void OnIngesting(Entity<SlimeGenesComponent> ent, ref IngestingEvent args)
    {
        if (!TryComp<SlimeComponent>(ent, out var slime)
            || !_proto.Resolve(ent.Comp.Diet, out var diet))
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        foreach (var reagent in args.Split.Contents)
        {
            if (!diet.Reagents.TryGetValue(reagent.Reagent.Prototype, out var shift))
                continue;

            var quantity = reagent.Quantity.Float();

            if (shift.MutationChance != 0)
            {
                slime.MutationChance = Math.Clamp(
                    slime.MutationChance + shift.MutationChance * quantity,
                    ent.Comp.MinMutationChance,
                    ent.Comp.MaxMutationChance);
            }

            if (shift.MitosisHunger != 0)
            {
                slime.MitosisHunger = Math.Clamp(
                    slime.MitosisHunger + shift.MitosisHunger * quantity,
                    ent.Comp.MinMitosisHunger,
                    ent.Comp.MaxMitosisHunger);
            }

            if (slime.MaxOffspring < ent.Comp.MaxOffspring && rand.Prob(Math.Min(1f, shift.OffspringChance * quantity)))
                slime.MaxOffspring++;

            if (slime.ExtractsProduced < ent.Comp.MaxYield && rand.Prob(Math.Min(1f, shift.YieldChance * quantity)))
                slime.ExtractsProduced++;
        }

        Dirty(ent.Owner, slime);
    }

    /// <summary>
    /// Передаёт гены и черты родителя потомку со случайным отклонением и бросает потомку новые черты.
    /// </summary>
    public void Inherit(Entity<SlimeComponent?> parent, Entity<SlimeComponent?> child, IRobustRandom rand)
    {
        if (!Resolve(parent, ref parent.Comp) || !Resolve(child, ref child.Comp))
            return;

        var p = parent.Comp;
        var c = child.Comp;

        if (!TryComp<SlimeGenesComponent>(child, out var genes))
        {
            c.MutationChance = p.MutationChance;
            c.MaxOffspring = p.MaxOffspring;
            c.ExtractsProduced = p.ExtractsProduced;
            Dirty(child.Owner, c);
            return;
        }

        c.MutationChance = Math.Clamp(
            p.MutationChance + rand.NextFloat(-genes.MutationChanceDrift, genes.MutationChanceDrift),
            genes.MinMutationChance,
            genes.MaxMutationChance);
        c.MaxOffspring = Drift(p.MaxOffspring, genes.OffspringDriftChance, genes.MinOffspring, genes.MaxOffspring, rand);
        c.ExtractsProduced = Drift(p.ExtractsProduced, genes.YieldDriftChance, genes.MinYield, genes.MaxYield, rand);
        c.MitosisHunger = Math.Clamp(
            p.MitosisHunger * (1f + rand.NextFloat(-genes.MitosisHungerDrift, genes.MitosisHungerDrift)),
            genes.MinMitosisHunger,
            genes.MaxMitosisHunger);
        Dirty(child.Owner, c);

        if (TryComp<SlimeGenesComponent>(parent, out var parentGenes))
        {
            foreach (var trait in GetTraits(parentGenes))
            {
                if (rand.Prob(trait.InheritChance))
                    TryAddTrait((child, genes), trait.ID);
            }
        }

        RollTraits((child, genes), rand);
    }

    /// <summary>
    /// Бросает шанс появления каждой черты и добавляет выпавшие.
    /// </summary>
    public void RollTraits(Entity<SlimeGenesComponent?> ent, IRobustRandom rand)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var trait in _proto.EnumeratePrototypes<SlimeTraitPrototype>().OrderBy(t => t.ID))
        {
            if (rand.Prob(trait.AppearChance))
                TryAddTrait(ent, trait.ID);
        }
    }

    /// <summary>
    /// Добавляет черту и её компоненты, если у слайма ещё нет этой черты или конфликтующей с ней.
    /// </summary>
    public bool TryAddTrait(Entity<SlimeGenesComponent?> ent, ProtoId<SlimeTraitPrototype> trait)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || ent.Comp.Traits.Contains(trait)
            || !_proto.Resolve(trait, out var traitProto))
            return false;

        foreach (var existing in GetTraits(ent.Comp))
        {
            if (traitProto.Conflicts.Contains(existing.ID) || existing.Conflicts.Contains(trait))
                return false;
        }

        ent.Comp.Traits.Add(trait);
        EntityManager.AddComponents(ent, traitProto.Components, false);

        if (TryComp<SlimeComponent>(ent, out var slime) && _light.TryGetLight(ent, out var light))
            _light.SetColor(ent, slime.SlimeColor, light);

        Dirty(ent.Owner, ent.Comp);
        return true;
    }

    /// <summary>
    /// Шанс мутации слайма с учётом множителей черт.
    /// </summary>
    public float GetMutationChance(Entity<SlimeComponent?> slime)
    {
        if (!Resolve(slime, ref slime.Comp))
            return 0f;

        if (!TryComp<SlimeGenesComponent>(slime, out var genes))
            return slime.Comp.MutationChance;

        var chance = GetTraits(genes).Aggregate(slime.Comp.MutationChance, (current, trait) => current * trait.MutationChanceMultiplier);
        return Math.Clamp(chance, 0f, 1f);
    }

    /// <summary>
    /// Число экстрактов при перемалывании слайма с учётом бонусов черт.
    /// </summary>
    public int GetExtractYield(Entity<SlimeComponent?> slime)
    {
        if (!Resolve(slime, ref slime.Comp))
            return 0;

        if (!TryComp<SlimeGenesComponent>(slime, out var genes))
            return slime.Comp.ExtractsProduced;

        return Math.Max(genes.MinYield, slime.Comp.ExtractsProduced + GetTraits(genes).Sum(trait => trait.YieldBonus));
    }

    /// <summary>
    /// Максимальное число потомков при делении слайма с учётом бонусов черт.
    /// </summary>
    public int GetMaxOffspring(Entity<SlimeComponent?> slime)
    {
        if (!Resolve(slime, ref slime.Comp))
            return 0;

        if (!TryComp<SlimeGenesComponent>(slime, out var genes))
            return slime.Comp.MaxOffspring;

        return Math.Max(genes.MinOffspring, slime.Comp.MaxOffspring + GetTraits(genes).Sum(trait => trait.OffspringBonus));
    }

    /// <summary>
    /// Порог сытости для деления слайма с учётом множителей черт.
    /// </summary>
    public float GetMitosisHunger(Entity<SlimeComponent?> slime)
    {
        if (!Resolve(slime, ref slime.Comp))
            return 0f;

        if (!TryComp<SlimeGenesComponent>(slime, out var genes))
            return slime.Comp.MitosisHunger;

        var hunger = GetTraits(genes).Aggregate(slime.Comp.MitosisHunger, (current, trait) => current * trait.MitosisHungerMultiplier);
        return Math.Min(hunger, genes.MaxMitosisHunger);
    }

    private IEnumerable<SlimeTraitPrototype> GetTraits(SlimeGenesComponent genes)
    {
        foreach (var trait in genes.Traits)
        {
            if (_proto.Resolve(trait, out var traitProto))
                yield return traitProto;
        }
    }

    private static int Drift(int value, float chance, int min, int max, IRobustRandom rand)
    {
        if (rand.Prob(chance))
            value += rand.Next(2) == 0 ? -1 : 1;

        return Math.Clamp(value, min, max);
    }
}
