using System.Numerics;
using Content.Shared._BlackM.Rappel;
using Content.Shared.Buckle.Components;
using Content.Shared._vg.TileMovement;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.ActionBlocker;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._BlackM.Rappel;

public sealed class RappelSystem : SharedRappelSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const float MinSlotDistance = 0.95f;

    private const float MobClearance = 0.55f;

    private const float BodyClearance = 0.35f;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    private static readonly TimeSpan PreviewInterval = TimeSpan.FromMilliseconds(500);
    [Dependency] private readonly MobStateSystem _mobState = default!;
    private TimeSpan _nextPreview;
    private readonly Dictionary<EntityUid, ActiveDescent> _active = new();
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    private sealed class ActiveDescent
    {
        public EntityUid Rope;
        public string Channel = string.Empty;
        public LandingPoint Point = default!;
        public MapCoordinates Source;
        public MapCoordinates Landing;
        public Angle OriginalRotation;
        public bool HadActor;
        public EntityUid? DescentAudio;
        public EntityUid? DeployedRope;
        public bool Started;
        public bool RopeLanded;
        public bool Completed;
    }

    private readonly Dictionary<string, LandingPoint> _points = new();
    private readonly Dictionary<MapCoordinates, int> _nextSlots = new();
    private readonly Dictionary<MapCoordinates, int> _lastSlots = new();

    private static readonly Vector2[] LandingOffsets =
    {
        new(-1f, 1f), new(1f, -1f), new(1f, 1f), new(-1f, -1f),
    };

    private sealed class LandingPoint
    {
        public MapCoordinates Center;
        public float AngleOffset;
        public EntityUid Zone;
    }

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<RappelConsoleComponent>(RappelConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
            subs.Event<RappelConsoleSelectPointMessage>(OnSelectPoint);
            subs.Event<RappelConsoleClearPointMessage>(OnClearPoint);
            subs.Event<RappelConsoleMoveCameraMessage>(OnMoveCamera);
            subs.Event<RappelConsoleRecenterMessage>(OnRecenter);
        });

        SubscribeLocalEvent<RappelConsoleComponent, ComponentShutdown>(OnConsoleShutdown);

        SubscribeLocalEvent<RappelRopeComponent, MapInitEvent>(OnRopeMapInit);
        SubscribeLocalEvent<RappelRopeComponent, ActivateInWorldEvent>(OnRopeActivate);
        SubscribeLocalEvent<RappelRopeComponent, GetVerbsEvent<AlternativeVerb>>(OnRopeVerbs);
        SubscribeLocalEvent<RappelRopeComponent, RappelDoAfterEvent>(OnRappelDoAfter);
        SubscribeLocalEvent<RappelBallastComponent, GetVerbsEvent<AlternativeVerb>>(OnDecorVerbs);
    }

    #region Console

    private bool TryFindZone(string id, out Entity<RappelZoneComponent> zone, out MapCoordinates coords)
    {
        var query = EntityQueryEnumerator<RappelZoneComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Id != id || xform.MapID == MapId.Nullspace)
                continue;

            zone = (uid, comp);
            coords = _xform.GetMapCoordinates(uid, xform);
            return true;
        }

        zone = default;
        coords = MapCoordinates.Nullspace;
        return false;
    }

    private EntityUid? EnsureCamera(Entity<RappelConsoleComponent> console)
    {
        if (console.Comp.Camera is { } existing && !TerminatingOrDeleted(existing))
            return existing;

        if (!TryFindZone(console.Comp.ZoneId, out _, out var coords))
            return null;

        var camera = Spawn(null, coords);
        var eye = EnsureComp<EyeComponent>(camera);
        _eye.SetDrawFov(camera, false, eye);
        _eye.SetDrawLight((camera, eye), true);
        _eye.SetZoom(camera, new Vector2(console.Comp.CameraZoom), eye);
        _metaData.SetEntityName(camera, $"rappel camera ({console.Comp.ZoneId})");

        console.Comp.Camera = camera;
        return camera;
    }

    private void OnUiOpened(Entity<RappelConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        var camera = EnsureCamera(ent);

        if (camera != null && TryComp<ActorComponent>(args.Actor, out var actor))
        {
            ent.Comp.Viewers.Add(args.Actor);
            _viewSubscriber.AddViewSubscriber(camera.Value, actor.PlayerSession);
        }

        UpdateConsoleUi(ent);
    }

    private void OnUiClosed(Entity<RappelConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        RemoveViewer(ent, args.Actor);
    }

    private void RemoveViewer(Entity<RappelConsoleComponent> ent, EntityUid viewer)
    {
        ent.Comp.Viewers.Remove(viewer);

        if (ent.Comp.Camera is not { } camera)
            return;

        if (TryComp<ActorComponent>(viewer, out var actor))
            _viewSubscriber.RemoveViewSubscriber(camera, actor.PlayerSession);

        if (ent.Comp.Viewers.Count == 0)
        {
            QueueDel(camera);
            ent.Comp.Camera = null;
        }
    }

    private void OnConsoleShutdown(Entity<RappelConsoleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Camera is { } camera)
            QueueDel(camera);

        ent.Comp.Camera = null;
        ent.Comp.Viewers.Clear();
    }

    private void OnSelectPoint(Entity<RappelConsoleComponent> ent, ref RappelConsoleSelectPointMessage args)
    {
        var user = args.Actor;

        if (!TryFindZone(ent.Comp.ZoneId, out var zone, out var zoneCoords))
        {
            _popup.PopupEntity(Loc.GetString("rappel-console-no-zone"), ent, user, PopupType.SmallCaution);
            return;
        }

        if (!float.IsFinite(args.Position.X) || !float.IsFinite(args.Position.Y) ||
            Vector2.Distance(args.Position, zoneCoords.Position) > zone.Comp.Radius)
        {
            _popup.PopupEntity(Loc.GetString("rappel-console-out-of-zone"), ent, user, PopupType.SmallCaution);
            return;
        }

        var target = new MapCoordinates(args.Position, zoneCoords.MapId);
        if (!TryGetLandingTile(target, out var center))
        {
            _popup.PopupEntity(Loc.GetString("rappel-console-bad-point"), ent, user, PopupType.SmallCaution);
            return;
        }

        var selected = new LandingPoint
        {
            Center = center,
            Zone = zone.Owner,
            AngleOffset = 0f,
        };
        if (!IsCommonLandingFree(selected))
        {
            _popup.PopupEntity(Loc.GetString("rappel-console-bad-point"), ent, user, PopupType.SmallCaution);
            return;
        }
        if (_points.TryGetValue(ent.Comp.Channel, out var previous) &&
            previous.Center.MapId == center.MapId &&
            Vector2.DistanceSquared(previous.Center.Position, center.Position) < 0.0001f &&
            previous.Zone == zone.Owner)
            selected = previous;
        _points[ent.Comp.Channel] = selected;

        SetRopesLinked(ent.Comp.Channel, true);
        _popup.PopupEntity(Loc.GetString("rappel-console-point-set"), ent, user);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_end.ogg"), ent);
        UpdateChannelConsoles(ent.Comp.Channel);
    }

    private void OnClearPoint(Entity<RappelConsoleComponent> ent, ref RappelConsoleClearPointMessage args)
    {
        if (!_points.Remove(ent.Comp.Channel))
            return;

        SetRopesLinked(ent.Comp.Channel, false);
        _popup.PopupEntity(Loc.GetString("rappel-console-point-cleared"), ent, args.Actor);
        UpdateChannelConsoles(ent.Comp.Channel);
    }

    private void OnMoveCamera(Entity<RappelConsoleComponent> ent, ref RappelConsoleMoveCameraMessage args)
    {
        if (ent.Comp.Camera is not { } camera || TerminatingOrDeleted(camera))
            return;

        if (!TryFindZone(ent.Comp.ZoneId, out var zone, out var zoneCoords))
            return;

        if (!float.IsFinite(args.Position.X) || !float.IsFinite(args.Position.Y))
            return;

        var offset = args.Position - zoneCoords.Position;
        if (offset.Length() > zone.Comp.Radius)
            offset = Vector2.Normalize(offset) * zone.Comp.Radius;

        _xform.SetMapCoordinates(camera, new MapCoordinates(zoneCoords.Position + offset, zoneCoords.MapId));
    }

    private void OnRecenter(Entity<RappelConsoleComponent> ent, ref RappelConsoleRecenterMessage args)
    {
        if (ent.Comp.Camera is not { } camera || !TryFindZone(ent.Comp.ZoneId, out _, out var zoneCoords))
            return;

        var pos = _points.TryGetValue(ent.Comp.Channel, out var point) ? point.Center : zoneCoords;
        _xform.SetMapCoordinates(camera, pos);
    }

    private void SetRopesLinked(string channel, bool linked)
    {
        var query = EntityQueryEnumerator<RappelRopeComponent>();
        while (query.MoveNext(out var uid, out var rope))
        {
            if (rope.Channel != channel || rope.Linked == linked)
                continue;

            rope.Linked = linked;
            Dirty(uid, rope);
            _appearance.SetData(uid, RappelRopeVisuals.Linked, linked);

            if (linked)
                _audio.PlayPvs(rope.HookSound, uid);
        }
    }

    private int CountRopes(string channel)
    {
        var count = 0;
        var query = EntityQueryEnumerator<RappelRopeComponent>();
        while (query.MoveNext(out _, out var rope))
        {
            if (rope.Channel == channel)
                count++;
        }

        return count;
    }

    private void UpdateChannelConsoles(string channel)
    {
        var query = EntityQueryEnumerator<RappelConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.Channel == channel)
                UpdateConsoleUi((uid, console));
        }
    }

    private void UpdateConsoleUi(Entity<RappelConsoleComponent> ent)
    {
        var found = TryFindZone(ent.Comp.ZoneId, out var zone, out var zoneCoords);
        _points.TryGetValue(ent.Comp.Channel, out var point);

        var zoneName = found && zone.Comp.ZoneName is { } name
            ? Loc.GetString(name)
            : ent.Comp.ZoneId;

        BuildPreview(point, ent.Comp.Channel, out var available, out var blocked, out var reserved);
        var state = new RappelConsoleBoundUserInterfaceState(
            GetNetEntity(ent.Comp.Camera),
            zoneName,
            zoneCoords.Position,
            found ? zone.Comp.Radius : 0f,
            point?.Center.Position,
            GetChannelSpacing(ent.Comp.Channel),
            point?.AngleOffset ?? 0f,
            CountRopes(ent.Comp.Channel),
            ent.Comp.CameraZoom,
            ent.Comp.MaxZoom,
            found)
        {
            AvailableSlots = available,
            BlockedSlots = blocked,
            ReservedSlots = reserved,
        };

        _ui.SetUiState(ent.Owner, RappelConsoleUiKey.Key, state);
    }

    private float GetChannelSpacing(string channel)
    {
        return 1f;
    }

    #endregion

    #region Rope

    private void OnRopeMapInit(Entity<RappelRopeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Linked = _points.ContainsKey(ent.Comp.Channel);
        Dirty(ent);
        _appearance.SetData(ent, RappelRopeVisuals.Linked, ent.Comp.Linked);
    }

    private void OnRopeActivate(Entity<RappelRopeComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = TryStartRappel(ent, args.User);
    }

    private void OnRopeVerbs(Entity<RappelRopeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rappel-rope-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_BlackM/Interface/VerbIcons/rappel.png")),
            Act = () => TryStartRappel(ent, user),
            Priority = 2,
        });
    }

    private void OnDecorVerbs(Entity<RappelBallastComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;
        var user = args.User;
        var uid = ent.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rappel-decor-remove"),
            Act = () => RemoveDecor(uid, user),
            Priority = 2,
        });
    }

    private void RemoveDecor(EntityUid uid, EntityUid user)
    {
        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(user) ||
            !HasComp<RappelBallastComponent>(uid) ||
            !_interaction.InRangeUnobstructed(user, uid) ||
            !_actionBlocker.CanInteract(user, uid))
            return;
        _popup.PopupEntity(Loc.GetString("rappel-decor-removed"), uid, user);
        QueueDel(uid);
    }

    public bool TryStartRappel(Entity<RappelRopeComponent> rope, EntityUid user)
    {
        if (TerminatingOrDeleted(user) || TerminatingOrDeleted(rope.Owner) ||
            HasComp<RappellingComponent>(user) || _active.ContainsKey(user) ||
            (HasComp<MobStateComponent>(user) && !_mobState.IsAlive(user)))
            return false;

        if (!ValidSettings(rope.Comp))
            return false;

        if (!_points.ContainsKey(rope.Comp.Channel))
        {
            _popup.PopupEntity(Loc.GetString("rappel-rope-not-linked"), rope, user, PopupType.SmallCaution);
            return false;
        }

        if (TryComp<BuckleComponent>(user, out var buckle) && buckle.Buckled)
        {
            _popup.PopupEntity(Loc.GetString("rappel-rope-buckled"), rope, user, PopupType.SmallCaution);
            return false;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, rope.Comp.HookDelay, new RappelDoAfterEvent(), rope, target: rope)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 1.5f,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _audio.PlayPvs(rope.Comp.HookSound, rope);
        _popup.PopupEntity(Loc.GetString("rappel-rope-hooking", ("user", Name(user))), user, PopupType.Small);
        return true;
    }

    private void OnRappelDoAfter(Entity<RappelRopeComponent> rope, ref RappelDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var user = args.User;

        if (TerminatingOrDeleted(user) || TerminatingOrDeleted(rope.Owner) ||
            HasComp<RappellingComponent>(user) || _active.ContainsKey(user) ||
            (HasComp<MobStateComponent>(user) && !_mobState.IsAlive(user)) ||
            (TryComp<BuckleComponent>(user, out var buckle) && buckle.Buckled) ||
            !ValidSettings(rope.Comp))
            return;

        if (!_points.TryGetValue(rope.Comp.Channel, out var point))
        {
            _popup.PopupEntity(Loc.GetString("rappel-rope-not-linked"), rope, user, PopupType.SmallCaution);
            return;
        }

        var source = _xform.GetMapCoordinates(user);
        if (!IsTileFree(source))
            return;

        if (!TryAllocateLanding(point, out var landing))
        {
            _popup.PopupEntity(Loc.GetString("rappel-marker-blocked"), rope, user, PopupType.SmallCaution);
            return;
        }
        var active = new ActiveDescent
        {
            Rope = rope.Owner,
            Channel = rope.Comp.Channel,
            Point = point,
            Source = source,
            Landing = landing,
            OriginalRotation = Transform(user).LocalRotation,
            HadActor = HasComp<ActorComponent>(user),
        };
        var deployed = Spawn("BlackMRappelActiveRope", landing);
        active.DeployedRope = deployed;
        var visual = EnsureComp<RappelDeployedRopeComponent>(deployed);
        visual.DeployStart = Timing.CurTime;
        visual.DeployDuration = rope.Comp.RopeDeployTime;
        visual.Height = rope.Comp.DropHeight;
        visual.Landed = false;
        visual.InUse = true;
        Dirty(deployed, visual);
        var delay = TimeSpan.FromSeconds(rope.Comp.RopeDeployTime);

        _active.Add(user, active);
        StopPulling(user);
        ResetDescentMovement(user);

        var rappel = EnsureComp<RappellingComponent>(user);
        rappel.Preparing = false;
        rappel.StartTime = Timing.CurTime + delay;
        rappel.Duration = rope.Comp.DescentTime;
        rappel.Height = visual.Height;
        rappel.SwaySeed = _random.NextFloat() * MathF.Tau;
        rappel.LandSound = rope.Comp.LandSound;
        Dirty(user, rappel);

        active.Started = true;
        TeleportRappeller(user, active.Landing);
        _xform.SetWorldRotation(Transform(user), Angle.FromDegrees(180));
        SpawnCeilingBreach(active.Landing, rope.Comp);

        UpdateChannelConsoles(rope.Comp.Channel);
    }

    private void SpawnCeilingBreach(MapCoordinates landing, RappelRopeComponent rope)
    {
        if (!rope.CeilingCollapse)
            return;

        var now = Timing.CurTime;
        var height = MathF.Max(rope.DropHeight, 1f) + 0.4f;
        var radius = Math.Clamp(rope.CeilingDebrisRadius, 0.3f, 4f);

        var breach = Spawn("BlackMRappelCeilingBreach", landing);
        _xform.AttachToGridOrMap(breach);
        var comp = EnsureComp<RappelCeilingBreachComponent>(breach);
        comp.StartTime = now;
        Dirty(breach, comp);

        if (rope.CeilingDebris.Count > 0)
        {
            for (var i = 0; i < Math.Clamp(rope.CeilingDebrisCount, 0, 16); i++)
            {
                SpawnFallingDebris(_random.Pick(rope.CeilingDebris), landing, radius * 0.35f, radius,
                    now + TimeSpan.FromSeconds(_random.NextFloat() * 0.45f), height);
            }
        }

        if (rope.CeilingSmallDebris.Count > 0)
        {
            for (var i = 0; i < Math.Clamp(rope.CeilingSmallDebrisCount, 0, 32); i++)
            {
                SpawnFallingDebris(_random.Pick(rope.CeilingSmallDebris), landing, 0f, radius,
                    now + TimeSpan.FromSeconds(_random.NextFloat() * 0.8f), height);
            }
        }

        var dustCount = Math.Clamp(rope.CeilingDustCount, 0, 16);
        for (var i = 0; i < dustCount; i++)
        {
            SpawnDust(rope.CeilingDust, landing, _random.NextVector2(0.5f),
                now + TimeSpan.FromSeconds(_random.NextFloat() * 0.25f),
                height, height * (0.35f + _random.NextFloat() * 0.25f), _random.NextVector2(0.5f));

            var dir = _random.NextAngle().ToVec();
            SpawnDust(rope.CeilingDust, landing, dir * (0.2f + _random.NextFloat() * 0.5f),
                now + TimeSpan.FromSeconds(0.55f + _random.NextFloat() * 0.3f),
                0.1f, -0.2f, dir * (0.6f + _random.NextFloat() * 0.6f));
        }

        if (rope.CeilingCollapseSound != null)
            _audio.PlayPvs(rope.CeilingCollapseSound, breach);
    }

    private void SpawnFallingDebris(EntProtoId proto, MapCoordinates landing, float minRadius, float maxRadius,
        TimeSpan start, float height)
    {
        var offset = _random.NextAngle().ToVec() * (minRadius + _random.NextFloat() * (maxRadius - minRadius));
        var uid = Spawn(proto, new MapCoordinates(landing.Position + offset, landing.MapId));
        _xform.AttachToGridOrMap(uid);
        var debris = EnsureComp<RappelFallingDebrisComponent>(uid);
        debris.StartTime = start;
        debris.FallHeight = height + _random.NextFloat() * 0.8f;
        debris.FallTime = MathF.Sqrt(2f * debris.FallHeight / 24f);
        debris.Spin = (_random.NextFloat() * 2f - 1f) * 540f;
        debris.FinalRotation = _random.NextAngle();
        debris.Drift = (_random.NextFloat() * 2f - 1f) * 0.6f;
        debris.ScaleMultiplier = 0.85f + _random.NextFloat() * 0.3f;
        Dirty(uid, debris);
        var despawn = EnsureComp<Robust.Shared.Spawners.TimedDespawnComponent>(uid);
        despawn.Lifetime = MathF.Max(0f, (float) (start - Timing.CurTime).TotalSeconds) + debris.FallTime +
            MathF.Max(0f, debris.FadeAfter) + MathF.Max(0.05f, debris.FadeTime) + 0.3f;
    }

    private void SpawnDust(EntProtoId proto, MapCoordinates landing, Vector2 offset, TimeSpan start,
        float height, float drop, Vector2 drift)
    {
        var uid = Spawn(proto, new MapCoordinates(landing.Position + offset, landing.MapId));
        _xform.AttachToGridOrMap(uid);
        var dust = EnsureComp<RappelDustComponent>(uid);
        dust.StartTime = start;
        dust.Height = height;
        dust.Drop = drop;
        dust.Drift = drift;
        Dirty(uid, dust);
    }

    private void EnsureLandingBallast(MapCoordinates position)
    {
        var query = EntityQueryEnumerator<RappelBallastComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!TerminatingOrDeleted(uid) && xform.MapID == position.MapId &&
                Vector2.DistanceSquared(_xform.GetMapCoordinates(uid, xform).Position,
                    position.Position) <= 0.0025f)
                return;
        }
        Spawn("BlackMRappelRopeDecor", position);
    }

    private void BeginDescent(EntityUid user, ActiveDescent active, RappellingComponent rappel)
    {
        if (!TryComp<RappelRopeComponent>(active.Rope, out var rope))
            return;

        active.RopeLanded = true;
        if (active.DeployedRope is { } deployed &&
            TryComp<RappelDeployedRopeComponent>(deployed, out var visual))
        {
            visual.InUse = true;
            Dirty(deployed, visual);
        }
        var audioParams = (rope.DescentSound?.Params ?? AudioParams.Default)
            .WithLoop(rope.LoopDescentSound).WithMaxDistance(8f);
        active.DescentAudio = _audio.PlayPvs(rope.DescentSound, user, audioParams)?.Entity;

        if (rope.ThrowFlashbang)
        {
            var initialAngle = _random.NextFloat() * MathF.Tau;
            for (var i = 0; i < rope.FlashbangCount; i++)
            {
                var grenade = Spawn("GrenadeFlashBang", active.Landing);
                if (TryComp<TimerTriggerComponent>(grenade, out var timer))
                    _trigger.ActivateTimerTrigger((grenade, timer), user);
                var angle = initialAngle + i * MathF.Tau / rope.FlashbangCount;
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                var distance = rope.FlashbangDistance * (0.85f + _random.NextFloat() * 0.3f);
                _throwing.TryThrow(grenade, direction * distance,
                    baseThrowSpeed: 5f, user: user, recoil: false);
            }
        }
        UpdateChannelConsoles(active.Channel);
    }

    private static bool ValidSettings(RappelRopeComponent rope)
    {
        return rope.FlashbangCount >= 1 && rope.FlashbangCount <= 12 &&
            float.IsFinite(rope.RopeDeployTime) && rope.RopeDeployTime >= 0.1f && rope.RopeDeployTime <= 30f &&
            float.IsFinite(rope.FlashbangDistance) && rope.FlashbangDistance >= 0.1f && rope.FlashbangDistance <= 10f &&
            float.IsFinite(rope.DescentTime) && rope.DescentTime >= 0.1f && rope.DescentTime <= 120f &&
            float.IsFinite(rope.DropHeight) && rope.DropHeight >= 0f && rope.DropHeight <= 64f &&
            float.IsFinite(rope.SlotHoldTime) && rope.SlotHoldTime >= 0f && rope.SlotHoldTime <= 120f;
    }

    private void TeleportRappeller(EntityUid uid, MapCoordinates coordinates)
    {
        _xform.SetMapCoordinates(uid, coordinates);
        _xform.AttachToGridOrMap(uid);
        ResetDescentMovement(uid);
        Dirty(uid, Transform(uid));
    }

    private void ResetDescentMovement(EntityUid uid)
    {
        if (TryComp<PhysicsComponent>(uid, out var body))
        {
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);
            _physics.SetAngularVelocity(uid, 0f, body: body);
            Dirty(uid, body);
        }
        if (TryComp<TileMovementComponent>(uid, out var tile))
        {
            var xform = Transform(uid);
            tile.SlideActive = false;
            tile.FailureSlideActive = false;
            tile.MovementKeyInitialDownTime = null;
            tile.CurrentSlideMoveButtons = MoveButtons.None;
            tile.WasWeightlessLastTick = false;
            tile.LastTickLocalCoordinates = null;
            tile.Origin = xform.Coordinates;
            tile.Destination = xform.LocalPosition;
            Dirty(uid, tile);
        }
    }

    private void StopPulling(EntityUid user)
    {
        if (TryComp<PullableComponent>(user, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(user, pullable);

        if (TryComp<PullerComponent>(user, out var puller) &&
            puller.Pulling is { } pulled &&
            TryComp<PullableComponent>(pulled, out var pulledComp))
        {
            _pulling.TryStopPull(pulled, pulledComp, user);
        }
    }

    #endregion

    #region Slots

    private bool IsInsideZone(LandingPoint point, MapCoordinates coords)
    {
        if (TerminatingOrDeleted(point.Zone) ||
            !TryComp<RappelZoneComponent>(point.Zone, out var zone) ||
            !float.IsFinite(zone.Radius) || zone.Radius < BodyClearance)
            return false;

        var center = _xform.GetMapCoordinates(point.Zone);
        return center.MapId == point.Center.MapId && coords.MapId == center.MapId &&
            Vector2.Distance(coords.Position, center.Position) <= zone.Radius - BodyClearance;
    }

    private bool HasMob(MapCoordinates coords, EntityUid? ignore = null)
    {
        foreach (var uid in _lookup.GetEntitiesInRange<MobStateComponent>(coords, MobClearance))
        {
            if (uid != ignore && !TerminatingOrDeleted(uid))
                return true;
        }
        return false;
    }

    private bool IsCommonLandingFree(LandingPoint point)
    {
        return GetLandingSlots(point).Any(slot =>
            IsInsideZone(point, slot) && IsFootprintFree(slot, ignoreMobs: true));
    }

    private IEnumerable<MapCoordinates> GetLandingSlots(LandingPoint point)
    {
        if (!_mapManager.TryFindGridAt(point.Center, out var gridUid, out var grid))
            yield break;
        var (_, rotation) = _xform.GetWorldPositionRotation(Transform(gridUid));
        foreach (var offset in LandingOffsets)
            yield return new MapCoordinates(point.Center.Position +
                rotation.RotateVec(offset * grid.TileSize), point.Center.MapId);
    }

    private bool IsSlotReserved(MapCoordinates coords, EntityUid? ignore = null)
    {
        return _active.Any(pair => pair.Key != ignore &&
            pair.Value.Landing.MapId == coords.MapId &&
            Vector2.DistanceSquared(pair.Value.Landing.Position, coords.Position) <
                MinSlotDistance * MinSlotDistance);
    }

    private bool IsLandingFree(LandingPoint point, MapCoordinates coords, EntityUid? ignore = null)
    {
        return IsInsideZone(point, coords) && !IsSlotReserved(coords, ignore) &&
            IsFootprintFree(coords, ignore) && !HasMob(coords, ignore);
    }

    private bool TryAllocateLanding(LandingPoint point, out MapCoordinates landing)
    {
        landing = default;
        var slots = GetLandingSlots(point).ToArray();
        if (slots.Length != LandingOffsets.Length)
            return false;
        _nextSlots.TryGetValue(point.Center, out var start);
        var hasLast = _lastSlots.TryGetValue(point.Center, out var last);
        for (var i = 0; i < slots.Length; i++)
        {
            var index = (start + i) % slots.Length;
            if ((hasLast && index == last) || !IsLandingFree(point, slots[index]))
                continue;
            landing = slots[index];
            _lastSlots[point.Center] = index;
            _nextSlots[point.Center] = (index + 1) % slots.Length;
            return true;
        }
        return false;
    }

    private bool IsFootprintFree(MapCoordinates coords, EntityUid? ignore = null, bool ignoreMobs = false)
    {
        if (!IsTileFree(coords, ignore, ignoreMobs))
            return false;
        for (var i = 0; i < 8; i++)
        {
            var angle = i * MathF.PI / 4f;
            var sample = coords.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * BodyClearance;
            if (!IsTileFree(new MapCoordinates(sample, coords.MapId), ignore, ignoreMobs))
                return false;
        }
        return true;
    }

    private void BuildPreview(LandingPoint? point, string channel,
        out Vector2[] available, out Vector2[] blocked, out Vector2[] reserved)
    {
        available = Array.Empty<Vector2>();
        blocked = Array.Empty<Vector2>();
        reserved = Array.Empty<Vector2>();
        if (point == null)
            return;
        var free = new List<Vector2>();
        var busy = new List<Vector2>();
        var taken = new List<Vector2>();
        var slots = GetLandingSlots(point).ToArray();
        var hasLast = _lastSlots.TryGetValue(point.Center, out var last);
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (IsSlotReserved(slot))
                taken.Add(slot.Position);
            else if ((!hasLast || i != last) && IsLandingFree(point, slot))
                free.Add(slot.Position);
            else
                busy.Add(slot.Position);
        }
        available = free.ToArray();
        blocked = busy.ToArray();
        reserved = taken.ToArray();
    }

    private bool TryGetLandingTile(MapCoordinates coords, out MapCoordinates center)
    {
        center = coords;
        if (!_mapManager.TryFindGridAt(coords, out var gridUid, out var grid))
            return false;

        var indices = _map.TileIndicesFor(gridUid, grid, coords);
        if (!_map.TryGetTileRef(gridUid, grid, indices, out var tile) ||
            tile.Tile.IsEmpty || _turf.IsSpace(tile))
            return false;

        center = _map.GridTileToWorld(gridUid, grid, indices);
        return true;
    }

    private bool IsTileFree(MapCoordinates coords, EntityUid? ignore = null, bool ignoreMobs = false)
    {
        if (!_mapManager.TryFindGridAt(coords, out var gridUid, out var grid))
            return false;

        var indices = _map.TileIndicesFor(gridUid, grid, coords);
        if (!_map.TryGetTileRef(gridUid, grid, indices, out var tileRef))
            return false;

        if (tileRef.Tile.IsEmpty || _turf.IsSpace(tileRef))
            return false;

        var gridXform = Transform(gridUid);
        var (gridPos, gridRot, matrix) = _xform.GetWorldPositionRotationMatrix(gridXform);
        var size = grid.TileSize;
        var localPos = new Vector2((indices.X + 0.5f) * size, (indices.Y + 0.5f) * size);
        var worldPos = Vector2.Transform(localPos, matrix);
        var tileBox = Box2.UnitCentered.Scale(0.95f * size);
        var worldBox = new Box2Rotated(tileBox.Translated(worldPos), gridRot, worldPos);
        tileBox = tileBox.Translated(localPos);
        foreach (var entity in _lookup.GetEntitiesIntersecting(gridUid, worldBox,
                     LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (entity == ignore || (ignoreMobs && HasComp<MobStateComponent>(entity)) ||
                TerminatingOrDeleted(entity) ||
                !TryComp<PhysicsComponent>(entity, out var body) || !body.CanCollide ||
                !TryComp<FixturesComponent>(entity, out var fixtures))
                continue;
            var (position, rotation) = _xform.GetWorldPositionRotation(Transform(entity));
            position = (-gridRot).RotateVec(position - gridPos);
            rotation -= gridRot;
            var fixtureTransform = new Robust.Shared.Physics.Transform(position, (float) rotation.Theta);
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard || (fixture.CollisionLayer & (int) CollisionGroup.MobMask) == 0)
                    continue;
                for (var child = 0; child < fixture.Shape.ChildCount; child++)
                {
                    var bounds = fixture.Shape.ComputeAABB(fixtureTransform, child);
                    if (!bounds.Intersects(tileBox))
                        continue;
                    var overlap = bounds.Intersect(tileBox);
                    if (overlap.Width > 0.001f && overlap.Height > 0.001f)
                        return false;
                }
            }
        }
        return true;
    }

    #endregion

    protected override void OnRappelShutdown(Entity<RappellingComponent> ent, ref ComponentShutdown args)
    {
        try
        {
            if (_active.Remove(ent.Owner, out var active))
            {
                StopDescentAudio(active);
                if (!TerminatingOrDeleted(ent.Owner) && active.Started && StillAtLanding(ent.Owner, active))
                    ReturnToSource(ent.Owner, active);
            }
        }
        finally
        {
            base.OnRappelShutdown(ent, ref args);
        }
    }

    private bool StillAtLanding(EntityUid uid, ActiveDescent active)
    {
        var coords = _xform.GetMapCoordinates(uid);
        return coords.MapId == active.Landing.MapId &&
            Vector2.DistanceSquared(coords.Position, active.Landing.Position) < 0.25f;
    }

    private void StopDescentAudio(ActiveDescent active)
    {
        if (active.DescentAudio is { } audio && !TerminatingOrDeleted(audio))
            _audio.Stop(audio);
        active.DescentAudio = null;
        if (active.DeployedRope is { } rope && !TerminatingOrDeleted(rope))
        {
            if (TryComp<RappelDeployedRopeComponent>(rope, out var visual))
            {
                visual.InUse = false;
                visual.Landed = true;
                Dirty(rope, visual);
            }
            QueueDel(rope);
        }
        active.DeployedRope = null;
    }

    private bool ReturnToSource(EntityUid uid, ActiveDescent active)
    {
        for (var i = 0; i < 64; i++)
        {
            var pos = active.Source.Position + RappelSlots.GetSpiralOffset(i, 0.5f, 0f);
            var coords = new MapCoordinates(pos, active.Source.MapId);
            if (!IsFootprintFree(coords, uid) || HasMob(coords, uid))
                continue;
            TeleportRappeller(uid, coords);
            _xform.SetLocalRotation(uid, active.OriginalRotation);
            return true;
        }
        _popup.PopupEntity(Loc.GetString("rappel-no-safe-return"), uid, uid, PopupType.SmallCaution);
        return false;
    }

    private void FinishDescent(EntityUid uid, bool abort)
    {
        if (!_active.Remove(uid, out var active))
            return;

        if (active.DescentAudio is { } sound && !TerminatingOrDeleted(sound))
            _audio.Stop(sound);
        active.DescentAudio = null;
        if (TerminatingOrDeleted(uid))
        {
            StopDescentAudio(active);
            return;
        }

        if (!TryComp<RappellingComponent>(uid, out var rappel))
        {
            if (active.Started && StillAtLanding(uid, active)) ReturnToSource(uid, active);
            StopDescentAudio(active);
            return;
        }

        try
        {
            if (!active.Started || !StillAtLanding(uid, active))
                abort = true;
            else if (!abort)
            {
                if (!IsLandingFree(active.Point, active.Landing, uid))
                    abort = true;
            }

            if (abort)
            {
                if (active.Started && StillAtLanding(uid, active)) ReturnToSource(uid, active);
                _popup.PopupEntity(Loc.GetString("rappel-interrupted"), uid, uid, PopupType.SmallCaution);
            }
            else
            {
                active.Completed = true;
                _xform.AttachToGridOrMap(uid);
                ResetDescentMovement(uid);
                EnsureLandingBallast(active.Landing);
                _audio.PlayPvs(rappel.LandSound, uid);
                if (TryComp<RappelRopeComponent>(active.Rope, out var rope))
                    _audio.PlayPvs(rope.UnhookSound, uid);
                _popup.PopupEntity(Loc.GetString("rappel-landed"), uid, uid);
            }
        }
        finally
        {
            StopDescentAudio(active);
            RemComp<RappellingComponent>(uid);
            _actionBlocker.UpdateCanMove(uid);
            if (TryComp<InputMoverComponent>(uid, out var mover))
                Dirty(uid, mover);
            Dirty(uid, Transform(uid));
        }
        UpdateChannelConsoles(active.Channel);
    }

    public override void Shutdown()
    {
        foreach (var uid in _active.Keys.ToArray())
            FinishDescent(uid, true);
        _points.Clear();
        _nextSlots.Clear();
        _lastSlots.Clear();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = Timing.CurTime;
        foreach (var uid in _active.Keys.ToArray())
        {
            if (!_active.TryGetValue(uid, out var active))
                continue;
            var abort = TerminatingOrDeleted(uid) || TerminatingOrDeleted(active.Rope) ||
                !IsInsideZone(active.Point, active.Landing) ||
                !_points.TryGetValue(active.Channel, out var current) || !ReferenceEquals(current, active.Point);
            if (!abort)
            {
                var expected = active.Started ? active.Landing : active.Source;
                var actual = _xform.GetMapCoordinates(uid);
                abort = actual.MapId != expected.MapId ||
                    Vector2.DistanceSquared(actual.Position, expected.Position) >= 0.25f ||
                    (active.DeployedRope is not { } deployed || TerminatingOrDeleted(deployed)) ||
                    (HasComp<MobStateComponent>(uid) && !_mobState.IsAlive(uid)) ||
                    (active.HadActor && !HasComp<ActorComponent>(uid)) ||
                    (TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled);
            }
            if (abort || !TryComp<RappellingComponent>(uid, out var rappel))
            {
                FinishDescent(uid, true);
                continue;
            }
            if (!active.Started)
            {
                FinishDescent(uid, true);
                continue;
            }
            if (!active.RopeLanded && now >= rappel.StartTime)
                BeginDescent(uid, active, rappel);
            ResetDescentMovement(uid);
            _xform.SetWorldRotation(Transform(uid), Angle.FromDegrees(180));
            if (now >= rappel.StartTime + TimeSpan.FromSeconds(rappel.Duration))
                FinishDescent(uid, false);
        }

        var query = EntityQueryEnumerator<RappellingComponent>();
        var stale = new List<EntityUid>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!_active.ContainsKey(uid)) stale.Add(uid);
        }
        foreach (var uid in stale)
            RemComp<RappellingComponent>(uid);

        if (now < _nextPreview)
            return;
        _nextPreview = now + PreviewInterval;
        var consoles = EntityQueryEnumerator<RappelConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (console.Viewers.Count > 0)
                UpdateConsoleUi((uid, console));
        }
    }
}
