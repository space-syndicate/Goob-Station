using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Localization;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// System for handling orbital strike item that spawns supply pods in a radius.
/// </summary>
public sealed class OrbitalStrikeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
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
    }

    private void OnUseInHand(Entity<OrbitalStrikeComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var component = entity.Comp;
        var user = args.User;

        var userXform = Transform(user);
        var centerCoords = userXform.Coordinates;
        var centerPos = _transformSystem.GetWorldPosition(userXform);

        var podCount = component.CurrentPodCount;
        var radius = component.CurrentRadius;
        var spawnInterval = component.SpawnInterval;

        for (var i = 0; i < podCount; i++)
        {
            var delay = spawnInterval * i;

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
                    var mapUid = _mapSystem.GetMapOrInvalid(userXform.MapID);
                    spawnCoords = new EntityCoordinates(mapUid, mapCoords.Position);
                }

                var podEntity = Spawn(component.PodPrototype, spawnCoords);
                var podComp = EnsureComp<OrbitalStrikePodComponent>(podEntity);
                
                var mode = component.AvailableExplosionModes.GetValueOrDefault(component.CurrentExplosionMode)
                           ?? new OrbitalExplosionMode(100f, 1f, 12f);
                podComp.ExplosionIntensity = mode.Intensity;
                podComp.ExplosionSlope = mode.Slope;
                podComp.ExplosionMaxTileIntensity = mode.MaxTileIntensity;

                var explosionCoords = _transformSystem.ToMapCoordinates(spawnCoords);
                var explosionDelay = component.ExplosionDelay;
                Timer.Spawn(explosionDelay, () =>
                {
                    _explosionSystem.QueueExplosion(
                        explosionCoords,
                        ExplosionSystem.DefaultExplosionPrototypeId,
                        podComp.ExplosionIntensity,
                        podComp.ExplosionSlope,
                        podComp.ExplosionMaxTileIntensity,
                        null,
                        maxTileBreak: 0);

                    if (Exists(podEntity))
                        QueueDel(podEntity);
                });
            });
        }

        _popup.PopupEntity(Loc.GetString(component.PopupLaunchLoc, ("count", podCount)), entity, user);
    }

    private void OnGetVerbs(Entity<OrbitalStrikeComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var component = entity.Comp;
        var user = args.User;

        var priority = 0;
        AddCountVerbs(entity, user, ref args, ref priority);
        AddRadiusVerbs(entity, user, ref args, ref priority);
        AddModeVerbs(entity, user, ref args, ref priority);
    }

    private void AddCountVerbs(Entity<OrbitalStrikeComponent> entity, EntityUid user, ref GetVerbsEvent<AlternativeVerb> args, ref int priority)
    {
        foreach (var count in entity.Comp.AvailablePodCounts)
        {
            var isSelected = entity.Comp.CurrentPodCount == count;
            var verb = new AlternativeVerb
            {
                Text = Loc.GetString(entity.Comp.VerbCountLoc, ("count", count), ("selected", isSelected)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    entity.Comp.CurrentPodCount = count;
                    Dirty(entity);
                    _popup.PopupEntity(Loc.GetString(entity.Comp.VerbCountLoc, ("count", count), ("selected", true)), entity, user);
                },
                Priority = priority
            };
            priority -= 1;
            args.Verbs.Add(verb);
        }
    }

    private void AddRadiusVerbs(Entity<OrbitalStrikeComponent> entity, EntityUid user, ref GetVerbsEvent<AlternativeVerb> args, ref int priority)
    {
        foreach (var radiusValue in entity.Comp.AvailableRadii)
        {
            var isSelected = Math.Abs(entity.Comp.CurrentRadius - radiusValue) < 0.1f;
            var verb = new AlternativeVerb
            {
                Text = Loc.GetString(entity.Comp.VerbRadiusLoc, ("radius", radiusValue), ("selected", isSelected)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    entity.Comp.CurrentRadius = radiusValue;
                    Dirty(entity);
                    _popup.PopupEntity(Loc.GetString(entity.Comp.VerbRadiusLoc, ("radius", radiusValue), ("selected", true)), entity, user);
                },
                Priority = priority
            };
            priority -= 1;
            args.Verbs.Add(verb);
        }
    }

    private void AddModeVerbs(Entity<OrbitalStrikeComponent> entity, EntityUid user, ref GetVerbsEvent<AlternativeVerb> args, ref int priority)
    {
        foreach (var modeName in entity.Comp.AvailableExplosionModes.Keys)
        {
            var isSelected = entity.Comp.CurrentExplosionMode == modeName;
            var verb = new AlternativeVerb
            {
                Text = Loc.GetString(entity.Comp.VerbModeLoc, ("mode", modeName), ("selected", isSelected)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    entity.Comp.CurrentExplosionMode = modeName;
                    Dirty(entity);
                    _popup.PopupEntity(Loc.GetString(entity.Comp.VerbModeLoc, ("mode", modeName), ("selected", true)), entity, user);
                },
                Priority = priority
            };
            priority -= 1;
            args.Verbs.Add(verb);
        }
    }

}

