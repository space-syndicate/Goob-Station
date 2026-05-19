using System.Linq;
using Content.Shared.Body;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.ChemistryRework;

public sealed partial class RemoveMarkEntityEffectSystem : EntityEffectSystem<VisualBodyComponent, RemoveMark>
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    protected override void Effect(Entity<VisualBodyComponent> entity, ref EntityEffectEvent<RemoveMark> args)
    {
        if (!_visualBody.TryGatherMarkingsData(entity.Owner, null, out _, out _, out var applied))
            return;

        if (applied == null)
            return;

        var updated = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var (category, organMarkings) in applied)
        {
            if (!organMarkings.TryGetValue(args.Effect.MarkingCategory, out var markings))
                continue;

            var clone = organMarkings.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<Marking>(kvp.Value));

            clone[args.Effect.MarkingCategory].RemoveAt(0);
            updated[category] = clone;
        }

        if (updated.Count > 0)
            _visualBody.ApplyMarkings(entity.Owner, updated);
    }
}
