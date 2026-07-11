using System.Collections.Generic;
using System.Linq;
using Content.Shared._CorvaxGoob.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Item;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CorvaxGoob.Xenoarchaeology.Artifact.XAE;

public sealed class ArtifactRandomTransformationSystem : BaseXAESystem<ArtifactRandomTransformationComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private readonly List<EntityPrototype> _validPrototypes = new();
    private bool _prototypesCached;

    private void CachePrototypes()
    {
        _validPrototypes.Clear();
        foreach (var proto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (CanEverTransformInto(proto))
            {
                _validPrototypes.Add(proto);
            }
        }
        _prototypesCached = true;
        Logger.Info($"[ArtifactTransform] Успешно закешировано предметов для превращения: {_validPrototypes.Count}");
    }

    private static bool CanEverTransformInto(EntityPrototype proto)
    {
        if (proto.Abstract || !proto.MapSavable || !proto.Components.ContainsKey("Item"))
            return false;

        var id = proto.ID.ToLower();
        if (id.Contains("admin") || id.Contains("debug") || id.Contains("test") || id.Contains("singularity") || id.Contains("tesla"))
            return false;

        if (!string.IsNullOrEmpty(proto.EditorSuffix))
        {
            var suffix = proto.EditorSuffix.ToLower();
            if (suffix.Contains("admin") || suffix.Contains("debug") || suffix.Contains("тест") || suffix.Contains("дебаг"))
                return false;
        }

        return true;
    }

    protected override void OnActivated(Entity<ArtifactRandomTransformationComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_prototypesCached)
            CachePrototypes();

        if (_validPrototypes.Count == 0)
        {
            Logger.Warning("[ArtifactTransform] Список валидных предметов пуст.");
            return;
        }

        var coords = args.Coordinates;
        var currentMapId = coords.GetMapId(EntityManager);
        var entities = _entityLookup.GetEntitiesInRange(coords, ent.Comp.Radius);
        int transformedCount = 0;

        foreach (var entity in entities)
        {
            if (entity == ent.Owner || !HasComp<ItemComponent>(entity))
                continue;

            var entXform = Transform(entity);
            if (entXform.MapID != currentMapId || _container.IsEntityInContainer(entity))
                continue;

            if (!_random.Prob(ent.Comp.TransformationPercentRatio))
                continue;

            var meta = MetaData(entity);
            var protoId = meta.EntityPrototype?.ID ?? "";
            if (string.IsNullOrEmpty(protoId) || ent.Comp.PrototypeIdBlacklistSubstrings.Any(b => protoId.ToLower().Contains(b.ToLower())))
                continue;

            var randomProto = _random.Pick(_validPrototypes);
            EntityManager.SpawnEntity(randomProto.ID, entXform.Coordinates);
            EntityManager.DeleteEntity(entity);
            transformedCount++;
        }

        Logger.Info($"[ArtifactTransform] Превращено предметов: {transformedCount}");
    }
}
// тест