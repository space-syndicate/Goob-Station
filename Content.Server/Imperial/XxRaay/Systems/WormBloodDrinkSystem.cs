using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormBloodDrinkSystem : SharedWormBloodDrinkSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly WormBloodSystem _wormBlood = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    protected override void DrainBlood(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, int amount)
    {
        if (!TryComp<BloodstreamComponent>(target, out var blood))
            return;

        if (!_solution.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var solution))
            return;

        var drain = FixedPoint2.Min(FixedPoint2.New(amount), solution.Volume);
        if (drain <= 0)
            return;

        if (!TryComp<WormBloodComponent>(worm, out var wormBlood))
            return;

        var space = wormBlood.MaxBlood - wormBlood.Blood;
        if (space <= 0)
            return;

        var maxDrainBySpace = FixedPoint2.New((int) Math.Ceiling(space / drinker.ConversionRatio));
        drain = FixedPoint2.Min(drain, maxDrainBySpace);
        if (drain <= 0)
            return;

        var gained = Math.Max(1, (int) (drain * drinker.ConversionRatio));
        gained = Math.Min(gained, space);
        if (gained <= 0)
            return;

        _solution.SplitSolution(blood.BloodSolution!.Value, drain);
        Dirty(target, blood);

        _wormBlood.TryAddBlood(worm, gained);
        _audio.PlayPvs(drinker.DrinkSound, worm);
    }
}
