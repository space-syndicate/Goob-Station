using Content.Shared.Item;
using Content.Shared.Whitelist;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Containers; // Добавили для работы с контейнерами
namespace Content.Server._CorvaxGoob.Xenoarchaeology.Artifact.XAE;

public sealed class ArtifactRandomTransformationSystem : BaseXAESystem<ArtifactRandomTransformationComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!; // Добавили синглтон контейнеров
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    private readonly List<EntityPrototype> _validPrototypes = new();
    private bool _prototypesCached = false;

    private void CachePrototypes(EntityUid ent)
    {
        _validPrototypes.Clear();
        foreach (var proto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (CanTransform(proto, ent))
            {
                _validPrototypes.Add(proto);
            }
        }
        _prototypesCached = true;
        Logger.Info($"[ArtifactTransform] Успешно закешировано предметов для превращения: {_validPrototypes.Count}");
    }

    private bool CanTransform(EntityPrototype proto, EntityUid ent)
    {
        if (proto.Abstract)
            return false;

        if (!proto.MapSavable)
            return false;

        if (!proto.Components.ContainsKey("Item"))
            return false;

        if (!TryComp<ArtifactRandomTransformationComponent>(ent, out var component))
            return false;

        var id = proto.ID.ToLower();
        if (component.PrototypeIdBlacklistSubstrings.Contains(id))
            return false;

        if (!string.IsNullOrEmpty(proto.EditorSuffix))
        {
            var suffix = proto.EditorSuffix.ToLower();
            if (component.PrototypeSuffixBlacklistSubstrings.Contains(suffix))
                return false;
        }

        return true;
    }

    protected override void OnActivated(Entity<ArtifactRandomTransformationComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_prototypesCached)
            CachePrototypes(ent);

        if (_validPrototypes.Count == 0)
        {
            Logger.Warning("[ArtifactTransform] Ошибка: Список валидных предметов пуст. Эффект прерван.");
            return;
        }

        EntityUid artifactUid = ent;
        var component = ent.Comp;
        var coords = args.Coordinates;

        // Получаем ID карты, на которой произошла активация
        var currentMapId = _transform.GetMapId(coords);

        // Ищем предметы в радиусе
        var entities = _entityLookup.GetEntitiesInRange<ItemComponent>(coords, component.Radius);
        int transformedCount = 0;

        foreach (var (entity, _) in entities)
        {
            if (entity == artifactUid)
                continue;

            if (!TryComp<MetaDataComponent>(entity, out var meta))
                continue;
            var protoId = meta.EntityPrototype?.ID ?? "";

            if (string.IsNullOrEmpty(protoId))
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

            if (_whitelistSystem.IsBlacklistPass(component.ComponentBlacklist, entity))
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