using System.Linq;
using Content.Shared.Imperial.NewRad.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Imperial.NewRad.EntitySystems;

public sealed partial class NewRadSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedRadiationSystem _radiationSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NewRadComponent, UseInHandEvent>(OnInHandActivation);
    }

    private void OnInHandActivation(EntityUid uid, NewRadComponent comp, UseInHandEvent ev)
    {
        var lookup = _lookup.GetEntitiesInRange(ev.User, 3);

        if (lookup is null)
            return;

        var nm = _random.Next(0, lookup.Count);
        var target = lookup.ElementAt(nm);
        var compRad = EnsureComp<RadiationSourceComponent>(target);

        _radiationSystem.SetIntensity(uid, _random.Next(4, 7));

#pragma warning disable RA0002
        compRad.Slope = _random.NextFloat(0.2f, 1f);
#pragma warning restore RA0002
    }
}
