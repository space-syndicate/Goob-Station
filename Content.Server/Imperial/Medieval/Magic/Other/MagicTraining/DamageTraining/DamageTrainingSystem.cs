using System.Linq;
using Robust.Shared.Random;
using Content.Shared.Projectiles;
using Content.Shared.EntityEffects;
using Content.Shared.Imperial.Medieval.Magic;

namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.DamageTraining;


/// <summary>
/// This system add currency after succes projectile hit event
/// </summary>
public sealed partial class DamageTrainingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageTrainingComponent, MedievalAfterSpawnEntityBySpellEvent>(OnProjectileSpawn);

        SubscribeLocalEvent<MedievalAddEssenceOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileSpawn(EntityUid uid, DamageTrainingComponent component, MedievalAfterSpawnEntityBySpellEvent args)
    {
        var addEssenceOnProjectileHitComponent = EnsureComp<MedievalAddEssenceOnProjectileHitComponent>(args.SpawnedEntity);

        addEssenceOnProjectileHitComponent.TrainingResults = component.TrainingResults.ToList();
        addEssenceOnProjectileHitComponent.Action = uid;
        addEssenceOnProjectileHitComponent.Performer = args.Performer;
    }

    private void OnProjectileHit(EntityUid uid, MedievalAddEssenceOnProjectileHitComponent component, ProjectileHitEvent args)
    {
        foreach (var trainingResult in component.TrainingResults)
        {
            if (!_random.Prob(trainingResult.Probability)) continue;

            _entityEffectsSystem.TryApplyEffect(args.Target, trainingResult, user: component.Performer);
        }
    }
}
