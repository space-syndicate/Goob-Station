using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// System for handling orbital strike item that spawns supply pods in a radius.
/// </summary>
public sealed class OrbitalStrikeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitalStrikeComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<OrbitalStrikeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<OrbitalStrikePodComponent, TimedDespawnEvent>(OnPodDespawn);
    }

    private void OnUseInHand(Entity<OrbitalStrikeComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var component = entity.Comp;
        var user = args.User;

        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        var centerCoords = userXform.Coordinates;
        var centerPos = _transformSystem.GetWorldPosition(userXform);

        var podCount = component.CurrentPodCount;
        var radius = component.CurrentRadius;
        var spawnInterval = component.SpawnInterval;

        for (int i = 0; i < podCount; i++)
        {
            var delay = TimeSpan.FromSeconds(i * spawnInterval);

            Timer.Spawn(delay, () =>
            {
                if (!Exists(entity))
                    return;

                var angle = _random.NextFloat(0, 2 * MathF.PI);
                var distance = _random.NextFloat(0, radius);
                var offset = new Vector2(
                    MathF.Cos(angle) * distance,
                    MathF.Sin(angle) * distance
                );

                var spawnPos = centerPos + offset;
                var mapCoords = new MapCoordinates(spawnPos, userXform.MapID);
                
                EntityCoordinates spawnCoords;
                if (_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
                {
                    var localPos = _mapSystem.WorldToLocal(gridUid, grid, mapCoords.Position);
                    spawnCoords = new EntityCoordinates(gridUid, localPos);
                    spawnCoords = _mapSystem.AlignToGrid(spawnCoords);
                }
                else
                {
                    var mapUid = _mapManager.GetMapEntityId(userXform.MapID);
                    spawnCoords = new EntityCoordinates(mapUid, mapCoords.Position);
                }

                var podEntity = Spawn("orbital_strike_pod_spawn", spawnCoords);
                var podComp = EnsureComp<OrbitalStrikePodComponent>(podEntity);
                
                var mode = component.AvailableExplosionModes.GetValueOrDefault(component.CurrentExplosionMode)
                           ?? new OrbitalStrikeComponent.ExplosionMode(100f, 1f, 12f);
                podComp.ExplosionIntensity = mode.Intensity;
                podComp.ExplosionSlope = mode.Slope;
                podComp.ExplosionMaxTileIntensity = mode.MaxTileIntensity;
                Dirty(podEntity, podComp);
            });
        }

        _popup.PopupEntity($"Запущено {podCount} ракет", entity, user);
    }

    private void OnGetVerbs(Entity<OrbitalStrikeComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var component = entity.Comp;
        var user = args.User;

        var priority = 0;
        foreach (var count in component.AvailablePodCounts)
        {
            var isSelected = component.CurrentPodCount == count;
            var verb = new AlternativeVerb
            {
                Text = isSelected 
                    ? $"Изменить количество ракет: {count} (выбрано)"
                    : $"Изменить количество ракет: {count}",
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    component.CurrentPodCount = count;
                    Dirty(entity);
                    _popup.PopupEntity($"Установлено количество ракет: {count}", entity, user);
                },
                Priority = priority
            };

            priority -= 1;
            args.Verbs.Add(verb);
        }

        foreach (var radiusValue in component.AvailableRadii)
        {
            var isSelected = Math.Abs(component.CurrentRadius - radiusValue) < 0.1f;
            var verb = new AlternativeVerb
            {
                Text = isSelected 
                    ? $"Изменить радиус: {radiusValue} тайлов (выбрано)"
                    : $"Изменить радиус: {radiusValue} тайлов",
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    component.CurrentRadius = radiusValue;
                    Dirty(entity);
                    _popup.PopupEntity($"Установлен радиус: {radiusValue} тайлов", entity, user);
                },
                Priority = priority
            };

            priority -= 1;
            args.Verbs.Add(verb);
        }

        foreach (var modeName in component.AvailableExplosionModes.Keys)
        {
            var isSelected = component.CurrentExplosionMode == modeName;
            var verb = new AlternativeVerb
            {
                Text = isSelected 
                    ? $"Изменить режим взрыва: {modeName} (выбрано)"
                    : $"Изменить режим взрыва: {modeName}",
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    component.CurrentExplosionMode = modeName;
                    Dirty(entity);
                    _popup.PopupEntity($"Установлен режим взрыва: {modeName}", entity, user);
                },
                Priority = priority
            };

            priority -= 1;
            args.Verbs.Add(verb);
        }
    }

    private void OnPodDespawn(Entity<OrbitalStrikePodComponent> entity, ref TimedDespawnEvent args)
    {
        if (!TryComp<MetaDataComponent>(entity, out var meta))
            return;

        var prototypeId = meta.EntityPrototype?.ID;
        
        if (prototypeId == "orbital_strike_pod_spawn")
        {
            if (!TryComp<TransformComponent>(entity, out var xform))
                return;

            var coords = _transformSystem.ToMapCoordinates(xform.Coordinates);
            _explosionSystem.QueueExplosion(
                coords,
                ExplosionSystem.DefaultExplosionPrototypeId,
                entity.Comp.ExplosionIntensity, 
                entity.Comp.ExplosionSlope,  
                entity.Comp.ExplosionMaxTileIntensity, 
                entity,
                maxTileBreak: 0
            );
        }
    }

}

