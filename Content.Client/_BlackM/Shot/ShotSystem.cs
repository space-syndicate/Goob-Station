using System.Numerics;
using Content.Shared._BlackM.CCVar;
using Content.Shared._BlackM.Shot;
using Content.Shared.Damage.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Reflect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Client._BlackM.Shot;

public sealed class ShotSystem : EntitySystem, IShotWorld
{
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private const float TrailSpacing = 0.22f;

    private const float MuzzleOffset = 0.45f;

    private const float MaxBackfill = 6f;

    private readonly Dictionary<EntityUid, ShotState> _states = new();
    private ShotOverlay _overlay = default!;
    private EntityQuery<OccluderComponent> _occluders;
    private EntityQuery<ProjectileComponent> _projectiles;
    private EntityQuery<ReflectiveComponent> _reflective;
    private EntityQuery<StaminaDamageOnCollideComponent> _staminaOnCollide;

    private static readonly string[] PhysicalDamage = { "Blunt", "Piercing", "Slash" };
    private static readonly string[] EnergyDamage = { "Heat", "Shock", "Radiation", "Cold" };

    private readonly Dictionary<(MapId, Vector2i), bool> _visCache = new();
    private float _visTimer;

    private bool _enabled = true;
    private bool _trails = true;

    public override void Initialize()
    {
        base.Initialize();
        _occluders = GetEntityQuery<OccluderComponent>();
        _projectiles = GetEntityQuery<ProjectileComponent>();
        _reflective = GetEntityQuery<ReflectiveComponent>();
        _staminaOnCollide = GetEntityQuery<StaminaDamageOnCollideComponent>();
        _overlay = new ShotOverlay(_cache, _states, this);
        SubscribeLocalEvent<ShotComponent, ComponentShutdown>(OnShutdown);

        Subs.CVar(_cfg, BlackMCVars.ShotEffectsEnabled, OnEnabledChanged, invokeImmediately: true);
        Subs.CVar(_cfg, BlackMCVars.ShotTrailsEnabled, OnTrailsChanged, invokeImmediately: true);
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;

        if (value)
        {
            if (!_overlays.HasOverlay<ShotOverlay>())
                _overlays.AddOverlay(_overlay);
            return;
        }

        _states.Clear();
        _visCache.Clear();
        _overlay.Clear();
        _overlays.RemoveOverlay(_overlay);
    }

    private void OnTrailsChanged(bool value)
    {
        _trails = value;
        if (!value)
            _overlay.ClearTrails();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay(_overlay);
        _overlay.Clear();
        _states.Clear();
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!_enabled)
            return;

        _visTimer += frameTime;
        if (_visTimer >= 0.2f)
        {
            _visTimer = 0f;
            _visCache.Clear();
        }

        var query = EntityQueryEnumerator<ShotComponent, TransformComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out var shot, out var xform, out var body))
        {
            if (body.LinearVelocity.LengthSquared() < 9f)
                continue;

            var pos = _xform.GetWorldPosition(xform);
            var dir = _xform.GetWorldRotation(xform).ToWorldVec();
            var isNew = !_states.TryGetValue(uid, out var st);
            var smoke = isNew ? shot.Smoke && !IsEnergy(uid) : st!.Smoke;
            var trail = _trails && smoke && shot.SmokeTrail;

            if (isNew)
            {
                var back = BackDistance(uid, xform.MapID, pos, dir);

                st = new ShotState
                {
                    Comp = shot,
                    Map = xform.MapID,
                    Pos = pos,
                    Dir = dir,
                    Visible = shot.Tracer && _random.Prob(shot.TracerChance),
                    Phase = _random.NextFloat(6.28f),
                    Dist = back,
                    Smoke = smoke,
                };
                _states[uid] = st;

                if (trail)
                {
                    _overlay.BeginTrail(uid, st.Map, shot);

                    var start = pos - dir * back;
                    for (var d = 0f; d < back; d += TrailSpacing)
                    {
                        _overlay.AddTrailPoint(uid, start + dir * d, dir, d);
                    }

                    _overlay.AddTrailPoint(uid, pos, dir, back);
                }
            }

            st = _states[uid];
            var step = (pos - st.Pos).Length();
            st.Dist += step;
            st.TrailAcc += step;
            st.Pos = pos;
            st.Dir = dir;
            st.Age += frameTime;

            if (trail)
            {
                st.TrailAcc = MathF.Min(st.TrailAcc, 4f);
                while (st.TrailAcc >= TrailSpacing)
                {
                    st.TrailAcc -= TrailSpacing;
                    _overlay.AddTrailPoint(uid, pos - dir * st.TrailAcc, dir, st.Dist - st.TrailAcc);
                }
            }
        }
    }

    private bool IsEnergy(EntityUid uid)
    {
        if (_reflective.TryComp(uid, out var reflective) &&
            (reflective.Reflective & ReflectType.Energy) != 0)
            return true;

        if (_staminaOnCollide.HasComp(uid))
            return true;

        if (!_projectiles.TryComp(uid, out var proj))
            return false;

        var dict = proj.Damage.DamageDict;
        foreach (var type in PhysicalDamage)
        {
            if (dict.TryGetValue(type, out var value) && value > 0)
                return false;
        }

        foreach (var type in EnergyDamage)
        {
            if (dict.ContainsKey(type))
                return true;
        }

        return false;
    }

    private float BackDistance(EntityUid uid, MapId map, Vector2 pos, Vector2 dir)
    {
        if (!_projectiles.TryComp(uid, out var proj))
            return 0f;

        var source = proj.Weapon ?? proj.Shooter;
        if (source is not { } src || TerminatingOrDeleted(src))
            return 0f;

        var srcXform = Transform(src);
        if (srcXform.MapID != map)
            return 0f;

        var srcPos = _xform.GetWorldPosition(srcXform);
        var back = Vector2.Dot(pos - srcPos, dir) - MuzzleOffset;
        return Math.Clamp(back, 0f, MaxBackfill);
    }

    private void OnShutdown(Entity<ShotComponent> ent, ref ComponentShutdown args)
    {
        _overlay.EndTrail(ent.Owner);
        _states.Remove(ent.Owner);
    }

    public bool Visible(MapId map, Vector2 pos)
    {
        var eye = _eye.CurrentEye;
        if (!eye.DrawFov)
            return true;

        MapCoordinates origin;
        if (_player.LocalEntity is { } local && !TerminatingOrDeleted(local))
            origin = _xform.GetMapCoordinates(local);
        else
            origin = eye.Position;

        if (origin.MapId != map)
            return false;

        var key = (map, new Vector2i((int) MathF.Floor(pos.X), (int) MathF.Floor(pos.Y)));
        if (_visCache.TryGetValue(key, out var cached))
            return cached;

        var visible = !IsOccluded(origin.Position, pos, map);
        _visCache[key] = visible;
        return visible;
    }

    private bool IsOccluded(Vector2 from, Vector2 to, MapId map)
    {
        var delta = to - from;
        var len = delta.Length();

        if (len > 40f)
            return true;

        if (len < 0.6f)
            return false;

        var ray = new CollisionRay(from, delta / len, (int) CollisionGroup.Opaque);

        foreach (var hit in _physics.IntersectRay(map, ray, len, returnOnFirstHit: false))
        {
            if (hit.Distance >= len - 0.5f)
                continue;

            if (_occluders.TryComp(hit.HitEntity, out var occluder) && occluder.Enabled)
                return true;
        }

        return false;
    }

    public bool Blocked(MapId map, Vector2 pos)
    {
        var coords = new MapCoordinates(pos, map);
        if (!_mapMan.TryFindGridAt(coords, out var gridUid, out var grid))
            return false;

        var tile = _map.CoordinatesToTile(gridUid, grid, _map.MapToGrid(gridUid, coords));
        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (enumerator.MoveNext(out var anchored))
        {
            if (anchored != null && _occluders.HasComp(anchored.Value))
                return true;
        }

        return false;
    }
}
