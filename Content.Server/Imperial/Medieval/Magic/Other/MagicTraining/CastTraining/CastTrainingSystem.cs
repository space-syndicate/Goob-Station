using Content.Shared.Imperial.Medieval.Magic;
using Robust.Shared.Random;
using Content.Shared.EntityEffects;

namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.CastTraining;


/// <summary>
/// This system add currency after succes spell cast
/// </summary>
public sealed partial class CastTrainingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CastTrainingComponent, MedievalAfterCastSpellEvent>(OnSpellCast);
    }

    private void OnSpellCast(EntityUid uid, CastTrainingComponent component, MedievalAfterCastSpellEvent args)
    {
        foreach (var trainingResult in component.TrainingResults)
        {
            if (!_random.Prob(trainingResult.Probability)) continue;

            _entityEffectsSystem.TryApplyEffect(args.Performer, trainingResult, user: args.Performer);
        }
    }
}
