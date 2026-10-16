using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Goobstation.Shared._Aquila.Xenobiology;
using Content.Goobstation.Shared._Aquila.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class SlimeMutationShuffleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeGenesComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SlimeGenesComponent> ent, ref MapInitEvent args)
    {
        if (!_cfg.GetCVar(AquilaCVars.XenobiologyShuffle)
            || !TryComp<SlimeComponent>(ent, out var slime)
            || _station.GetOwningStation(ent) is not { } station)
            return;

        var xenobiology = EnsureComp<StationXenobiologyComponent>(station);
        if (xenobiology.MutationGraph.Count == 0)
            xenobiology.MutationGraph = GenerateGraph();

        if (!xenobiology.MutationGraph.TryGetValue(slime.Breed, out var mutations))
            return;

        slime.PotentialMutations = new HashSet<ProtoId<BreedPrototype>>(mutations);
        Dirty(ent, slime);
    }

    private Dictionary<ProtoId<BreedPrototype>, HashSet<ProtoId<BreedPrototype>>> GenerateGraph()
    {
        var mapping = new Dictionary<ProtoId<BreedPrototype>, ProtoId<BreedPrototype>>();
        foreach (var group in _proto.EnumeratePrototypes<SlimeBreedGroupPrototype>())
        {
            if (!group.Shuffle)
                continue;

            var shuffled = new List<ProtoId<BreedPrototype>>(group.Breeds);
            _random.Shuffle(shuffled);

            for (var i = 0; i < group.Breeds.Count; i++)
            {
                mapping[group.Breeds[i]] = shuffled[i];
            }
        }

        var slimeName = Factory.GetComponentName<SlimeComponent>();
        var graph = new Dictionary<ProtoId<BreedPrototype>, HashSet<ProtoId<BreedPrototype>>>();
        foreach (var breed in _proto.EnumeratePrototypes<BreedPrototype>())
        {
            if (breed.Abstract
                || !breed.Components.TryGetComponent(slimeName, out var comp)
                || comp is not SlimeComponent slime)
                continue;

            var mutations = new HashSet<ProtoId<BreedPrototype>>();
            foreach (var mutation in slime.PotentialMutations)
            {
                mutations.Add(mapping.GetValueOrDefault(mutation, mutation));
            }

            graph[mapping.GetValueOrDefault(breed.ID, breed.ID)] = mutations;
        }

        return graph;
    }
}
