using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Item;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Log;
using Robust.Shared.Containers; // Добавили для работы с контейнерами
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class ArtifactRandomTransformationSystem : BaseXAESystem<ArtifactRandomTransformationComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!; // Добавили синглтон контейнеров

    private readonly List<EntityPrototype> _validPrototypes = new();
    private bool _prototypesCached = false;

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
        if (proto.Abstract)
            return false;

        if (!proto.MapSavable)
            return false;

        if (!proto.Components.ContainsKey("Item"))
            return false;

        var id = proto.ID.ToLower();
        if (id.Contains("admin") ||
            id.Contains("debug") ||
            id.Contains("test") ||
            id.Contains("singularity") ||
            id.Contains("tesla"))
        {
            return false;
        }

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
            Logger.Warning("[ArtifactTransform] Ошибка: Список валидных предметов пуст. Эффект прерван.");
            return;
        }

        EntityUid artifactUid = ent;
        var component = ent.Comp;
        var coords = args.Coordinates;
        
        // Получаем ID карты, на которой произошла активация
        var currentMapId = coords.GetMapId(EntityManager);

        // Ищем сущности в радиусе
        var entities = _entityLookup.GetEntitiesInRange(coords, component.Radius);
        int transformedCount = 0;

        foreach (var entity in entities)
        {
            if (entity == artifactUid)
                continue;

            // Проверяем, что это предмет
            if (!HasComp<ItemComponent>(entity))
                continue;

            var entXform = Transform(entity);
            
            // Исправлено: Проверяем, что предмет находится на той же карте
            if (entXform.MapID != currentMapId)
                continue;

            // Защита: Если предмет лежит в рюкзаке, шкафу или ящике — игнорируем его
            if (_container.IsEntityInContainer(entity))
                continue;

            // Проверка на шанс спавна
            if (!_random.Prob(component.TransformationPercentRatio))
                continue;

            var meta = MetaData(entity);
            var protoId = meta.EntityPrototype?.ID ?? "";

            if (string.IsNullOrEmpty(protoId))
                continue;

            if (component.PrototypeIdBlacklistSubstrings.Any(b => protoId.ToLower().Contains(b.ToLower())))
                continue;

            // Выбираем случайный предмет из кэша и заменяем
            var randomProto = _random.Pick(_validPrototypes);

            EntityManager.SpawnEntity(randomProto.ID, entXform.Coordinates);
            EntityManager.DeleteEntity(entity);
            transformedCount++;
        }

        Logger.Info($"[ArtifactTransform] Эффект активирован на {coords}. Превращено предметов: {transformedCount}");
    }
}