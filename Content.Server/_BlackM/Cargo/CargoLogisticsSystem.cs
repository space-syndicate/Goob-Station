using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.Cargo;
using Content.Shared.ActionBlocker;
using Content.Shared.Cargo;
using Content.Shared.Chasm;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Gravity;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using static Content.Shared._BlackM.Cargo.HeavyCargoTiming;

namespace Content.Server._BlackM.Cargo;

public enum CargoRoute : byte { NotConfigured, Accepted, Rejected }

public sealed class CargoLogisticsSystem : EntitySystem
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    private static readonly SoundPathSpecifier StartSound =
        new("/Audio/Machines/warning_buzzer.ogg", AudioParams.Default.WithVolume(-4f).WithMaxDistance(12f));
    private static readonly SoundPathSpecifier UnlockSound =
        new("/Audio/Machines/boltsup.ogg", AudioParams.Default.WithVolume(-1f));
    private static readonly SoundPathSpecifier LockSound =
        new("/Audio/Machines/boltsdown.ogg", AudioParams.Default.WithVolume(-1f));
    private static readonly SoundPathSpecifier DriveDownSound =
        new("/Audio/Machines/airlock_ext_open.ogg", AudioParams.Default.WithVolume(-2f).WithPitchScale(0.8f).WithMaxDistance(14f));
    private static readonly SoundPathSpecifier DriveUpSound =
        new("/Audio/Machines/airlock_ext_close.ogg", AudioParams.Default.WithVolume(-2f).WithPitchScale(0.8f).WithMaxDistance(14f));
    private static readonly SoundPathSpecifier HydraulicSound =
        new("/Audio/Mecha/sound_mecha_hydraulic.ogg", AudioParams.Default.WithVolume(-7f).WithPitchScale(0.85f));
    private static readonly SoundCollectionSpecifier BottomSound =
        new("MetalThud", AudioParams.Default.WithVolume(-3f).WithVariation(0.08f));
    private static readonly SoundPathSpecifier PaidSound =
        new("/Audio/Machines/high_tech_confirm.ogg", AudioParams.Default.WithVolume(-3f));
    private static readonly SoundPathSpecifier ArrivalSound =
        new("/Audio/Machines/chime.ogg", AudioParams.Default.WithVolume(-2f));
    private static readonly SoundPathSpecifier BlockedSound =
        new("/Audio/Machines/buzz-two.ogg", AudioParams.Default.WithVolume(-4f));
    private static readonly SoundPathSpecifier FallSound =
        new("/Audio/Effects/falling.ogg", AudioParams.Default.WithVolume(2f));

    private const float HoleDepth = 0.35f;

    private const float BlockedBuzzCooldown = 2.5f;

    private const float DriftSquared = 0.25f;

    private readonly List<Delivery> _deliveries = new();
    private readonly Dictionary<EntityUid, Transfer> _transfers = new();
    private readonly List<EntityUid> _remove = new();
    private readonly HashSet<EntityUid> _treeScratch = new();
    private float _scan;

    private sealed class Delivery(EntityUid station, CargoOrderData order, TimeSpan now)
    {
        public readonly EntityUid Station = station;
        public readonly CargoOrderData Order = order;
        public int Remaining = order.OrderQuantity;
        public readonly TimeSpan AcceptedAt = now;
    }

    private sealed class Cargo(EntityUid item, EntityUid? invoice, Vector2 start, bool canCollide,
        HashSet<EntityUid> snapshot, double quoted)
    {
        public readonly EntityUid Item = item;
        public readonly EntityUid? Invoice = invoice;

        public readonly Vector2 Start = start;
        public readonly bool CanCollide = canCollide;

        public readonly HashSet<EntityUid> Snapshot = snapshot;
        public readonly double Quoted = quoted;
        public Vector2 LastPosition = start;
    }

    private sealed class Transfer(EntityUid grid, Vector2 center, Vector2 dockPosition, EntityUid station, bool export)
    {
        public readonly EntityUid Grid = grid;
        public readonly Vector2 Center = center;
        public readonly Vector2 DockPosition = dockPosition;
        public readonly EntityUid Station = station;
        public readonly bool Export = export;
        public readonly List<Cargo> Items = new();
        public float Elapsed;
        public float BlockedTime;
        public float HealthCheck;
        public bool CargoReleased;
        public float BuzzCooldown;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoDockComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CargoDockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CargoDockComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CargoDockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            foreach (var transfer in _transfers.Values)
            {
                foreach (var cargo in transfer.Items)
                    ReleaseCargo(cargo);
            }
            _transfers.Clear();
            _deliveries.Clear();
            _scan = 0;
        });
    }

    private void OnStartup(EntityUid uid, CargoDockComponent comp, ComponentStartup args)
    {
        _appearance.SetData(uid, CargoVisuals.Export, comp.Mode == CargoDockMode.Export);
        _appearance.SetData(uid, CargoVisuals.Phase, comp.VisualPhase);
    }

    private void OnMapInit(EntityUid uid, CargoDockComponent comp, MapInitEvent args)
    {
        EnsureHood(uid, comp);
        SetState(uid, comp, CargoState.Idle);
        SetMachinePhase(uid, comp, CargoPhase.Idle, true);
    }

    private void OnShutdown(EntityUid uid, CargoDockComponent comp, ComponentShutdown args)
    {
        if (_transfers.Remove(uid, out var transfer))
            Abort(transfer);
        if (comp.Hood is { } hood && !TerminatingOrDeleted(hood))
            QueueDel(hood);
    }

    private void OnExamined(EntityUid uid, CargoDockComponent comp, ExaminedEvent args)
    {
        var key = comp.State switch
        {
            CargoState.Preparing => "cargo-lift-state-preparing",
            CargoState.Moving => "cargo-lift-state-moving",
            CargoState.Blocked => "cargo-lift-state-blocked",
            _ => "cargo-lift-state-idle",
        };
        args.PushMarkup(Loc.GetString(key));
        if (comp.FallThrough)
            args.PushMarkup(Loc.GetString("cargo-lift-shaft-warning"));
        if (Usable(uid, comp, out var grid))
        {
            EntityUid? blocker;
            if (_transfers.TryGetValue(uid, out var transfer))
                blocker = FindBlocker(uid, grid, transfer.Center, PadHalf(uid), OwnSet(transfer));
            else if (comp.Mode == CargoDockMode.Export)
                SelectExports(uid, comp, grid, out blocker, out _);
            else
                blocker = FindBlocker(uid, grid, PadCenter(uid), PadHalf(uid));
            if (blocker is { } found)
                args.PushMarkup(Loc.GetString("cargo-lift-examine-blocker", ("item", Identity.Entity(found, EntityManager))));
        }
        if (comp.Mode != CargoDockMode.Import)
            return;
        var station = _station.GetOwningStation(uid);
        var count = _deliveries.Where(d => d.Station == station).Sum(d => d.Remaining);
        args.PushMarkup(Loc.GetString("cargo-lift-queue", ("count", count)));
    }

    #region Geometry

    private (int Tiles, Vector2 Offset) PadShape(EntityUid uid) =>
        TryComp<CargoMachineVisualComponent>(uid, out var visual)
            ? (Math.Max(1, visual.PadTiles), visual.PadOffset)
            : (1, Vector2.Zero);

    private float PadHalf(EntityUid uid) => PadShape(uid).Tiles * 0.5f - 0.05f;

    private Vector2 PadCenter(EntityUid uid)
    {
        var xform = Transform(uid);
        return xform.LocalPosition + xform.LocalRotation.RotateVec(PadShape(uid).Offset);
    }

    private bool OnPad(EntityUid uid, Vector2 center, float half, Vector2 position)
    {
        var local = (-Transform(uid).LocalRotation).RotateVec(position - center);
        return MathF.Abs(local.X) <= half && MathF.Abs(local.Y) <= half;
    }

    private int Capacity(EntityUid uid, CargoDockComponent dock)
    {
        var tiles = PadShape(uid).Tiles;
        var limit = dock.MaxItems > 0 ? dock.MaxItems : tiles * tiles;
        return dock.Mode == CargoDockMode.Import ? Math.Clamp(limit, 1, tiles * tiles) : Math.Clamp(limit, 1, 100);
    }

    private List<Vector2> ImportSlots(EntityUid uid, Vector2 center)
    {
        var tiles = PadShape(uid).Tiles;
        var rotation = Transform(uid).LocalRotation;
        var slots = new List<Vector2>(tiles * tiles);
        var mid = (tiles - 1) * 0.5f;
        for (var y = 0; y < tiles; y++)
        for (var x = 0; x < tiles; x++)
            slots.Add(new Vector2(x - mid, y - mid));
        return slots.OrderBy(s => s.LengthSquared()).ThenBy(s => -s.Y).ThenBy(s => s.X)
            .Select(s => center + rotation.RotateVec(s)).ToList();
    }

    #endregion

    private bool Usable(EntityUid uid, CargoDockComponent comp, out EntityUid grid)
    {
        grid = default;
        if (TerminatingOrDeleted(uid))
            return false;
        var xform = Transform(uid);
        if (!xform.Anchored || xform.GridUid is not { } gridUid || xform.ParentUid != gridUid ||
            !this.IsPowered(uid, EntityManager))
            return false;
        grid = gridUid;
        return EnsureHood(uid, comp);
    }

    private bool EnsureHood(EntityUid uid, CargoDockComponent comp)
    {
        if (comp.Hood is { } hood)
        {
            if (!TerminatingOrDeleted(hood))
                QueueDel(hood);
            comp.Hood = null;
        }
        var xform = Transform(uid);
        return xform.GridUid is { } grid && xform.ParentUid == grid;
    }

    private int PendingImports(EntityUid station) =>
        _deliveries.Where(d => d.Station == station).Sum(d => d.Remaining)
        + _transfers.Values.Where(t => t.Station == station && !t.Export && !t.CargoReleased).Sum(t => t.Items.Count);

    public CargoRoute TryQueueDelivery(EntityUid station, EntityUid console, CargoOrderData order,
        out EntityUid destination, out string error)
    {
        destination = default;
        error = Loc.GetString("cargo-lift-unavailable");
        var grid = Transform(console).GridUid;
        var configured = false;
        var query = EntityQueryEnumerator<CargoDockComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dock, out var xform))
        {
            if (dock.Mode != CargoDockMode.Import || xform.GridUid != grid ||
                _station.GetOwningStation(uid) != station)
                continue;
            configured = true;
            if (!Usable(uid, dock, out _) || order.OrderQuantity <= 0)
                continue;
            if (PendingImports(station) + order.OrderQuantity > Math.Clamp(dock.QueueCapacity, 1, 200))
            {
                error = Loc.GetString("cargo-lift-queue-full");
                continue;
            }
            if (_deliveries.Any(d => d.Station == station && d.Order.OrderId == order.OrderId))
                return CargoRoute.Rejected;
            _deliveries.Add(new Delivery(station, order, _timing.CurTime));
            destination = uid;
            return CargoRoute.Accepted;
        }
        return configured ? CargoRoute.Rejected : CargoRoute.NotConfigured;
    }

    #region Export

    // blocker stops the lift, rejected is just left on the pad.
    private List<(EntityUid Item, double Price)> SelectExports(EntityUid uid, CargoDockComponent dock,
        EntityUid grid, out EntityUid? blocker, out EntityUid? rejected)
    {
        var picked = new List<(EntityUid, double)>();
        var taken = new HashSet<EntityUid>();
        var center = PadCenter(uid);
        var half = PadHalf(uid);
        var limit = Capacity(uid, dock);
        rejected = null;
        foreach (var item in PadEntities(uid, grid, center, half))
        {
            if (picked.Count < limit && _cargo.LiftCanExport(item, out var price))
            {
                picked.Add((item, price));
                taken.Add(item);
            }
            else if (rejected == null && IsVisibleJunk(item))
            {
                rejected = item;
            }
        }
        blocker = FindBlocker(uid, grid, center, half, taken);
        return picked;
    }

    private bool IsVisibleJunk(EntityUid ent) =>
        !HasComp<GhostComponent>(ent) && !HasComp<ChasmFallingComponent>(ent) &&
        TryComp<PhysicsComponent>(ent, out var body) && body.CanCollide;

    private string BlockedMessage(EntityUid blocker)
    {
        var name = Identity.Entity(blocker, EntityManager);
        if (HasComp<MobStateComponent>(blocker))
            return Loc.GetString("cargo-lift-export-blocked-mob", ("item", name));
        if (_cargo.LiftFindForbidden(blocker) is { } inner && inner != blocker)
            return Loc.GetString("cargo-lift-export-blocked-inside", ("item", name), ("inner", Identity.Entity(inner, EntityManager)));
        if (_cargo.LiftCanExport(blocker, out _))
            return Loc.GetString("cargo-lift-export-blocked-full", ("item", name));
        return Loc.GetString("cargo-lift-export-blocked-item", ("item", name));
    }

    private string RejectedMessage(EntityUid item)
    {
        var name = Identity.Entity(item, EntityManager);
        if (_cargo.LiftFindForbidden(item) is { } inner && inner != item)
            return Loc.GetString("cargo-lift-export-blocked-inside", ("item", name), ("inner", Identity.Entity(inner, EntityManager)));
        return Loc.GetString("cargo-lift-export-nothing", ("item", name));
    }

    public bool GetExportQuote(EntityUid grid, out int value, out int count, out bool ready)
    {
        value = 0;
        count = 0;
        ready = false;
        var configured = false;
        double total = 0;
        var query = EntityQueryEnumerator<CargoDockComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dock, out var xform))
        {
            if (dock.Mode != CargoDockMode.Export || xform.GridUid != grid)
                continue;
            configured = true;
            if (!Usable(uid, dock, out var dockGrid) || _transfers.ContainsKey(uid) || dock.CooldownLeft > 0)
                continue;
            var lots = SelectExports(uid, dock, dockGrid, out var blocker, out _);
            if (lots.Count == 0 || blocker != null)
                continue;
            ready = true;
            total += lots.Sum(l => l.Price);
            count += lots.Count;
        }
        value = (int) Math.Round(Math.Min(total, int.MaxValue));
        return configured;
    }

    public bool TryStartExports(EntityUid grid, EntityUid station, out string error)
    {
        error = Loc.GetString("cargo-lift-export-empty");
        if (!GetExportQuote(grid, out _, out _, out _))
            return false;
        var started = 0;
        EntityUid? blocker = null;
        EntityUid? rejected = null;
        var query = EntityQueryEnumerator<CargoDockComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dock, out var xform))
        {
            if (dock.Mode != CargoDockMode.Export || xform.GridUid != grid ||
                _station.GetOwningStation(uid) != station || !Usable(uid, dock, out var dockGrid) ||
                _transfers.ContainsKey(uid) || dock.CooldownLeft > 0)
                continue;
            var lots = SelectExports(uid, dock, dockGrid, out var padBlocker, out var padRejected);
            rejected ??= padRejected;
            if (padBlocker != null)
            {
                blocker ??= padBlocker;
                continue;
            }
            if (lots.Count == 0)
                continue;
            var transfer = new Transfer(dockGrid, PadCenter(uid), xform.LocalPosition, station, true);
            foreach (var (item, price) in lots)
                AddCargo(transfer, item, null, Transform(item).LocalPosition, price);
            Begin(uid, dock, transfer);
            started += lots.Count;
        }
        if (started > 0)
        {
            error = Loc.GetString("cargo-lift-export-started", ("count", started));
            if (blocker is { } stuck)
                error += "\n" + BlockedMessage(stuck);
            else if (rejected is { } left)
                error += "\n" + Loc.GetString("cargo-lift-export-left", ("item", Identity.Entity(left, EntityManager)));
        }
        else if (blocker is { } stuck)
            error = BlockedMessage(stuck);
        else if (rejected is { } left)
            error = RejectedMessage(left);
        return true;
    }

    #endregion

    private IEnumerable<EntityUid> PadEntities(EntityUid uid, EntityUid grid, Vector2 center, float half)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(new EntityCoordinates(grid, center), half * 1.415f + 0.1f))
        {
            if (ent == uid || TerminatingOrDeleted(ent) || HasComp<CargoTransitComponent>(ent))
                continue;
            var xform = Transform(ent);
            if (xform.ParentUid != grid || xform.Anchored || !OnPad(uid, center, half, xform.LocalPosition))
                continue;
            yield return ent;
        }
    }

    private bool LaneClear(EntityUid uid, EntityUid grid, Vector2 center, float half, IReadOnlySet<EntityUid>? own = null) =>
        FindBlocker(uid, grid, center, half, own) == null;

    private EntityUid? FindBlocker(EntityUid uid, EntityUid grid, Vector2 center, float half, IReadOnlySet<EntityUid>? own = null)
    {
        EntityUid? hard = null;
        foreach (var ent in PadEntities(uid, grid, center, half))
        {
            if (own != null && own.Contains(ent))
                continue;
            var mob = HasComp<MobStateComponent>(ent);
            if (!IsLaneBlocker(ent, mob))
                continue;
            if (mob)
                return ent;
            hard ??= ent;
        }
        return hard;
    }

    private bool IsLaneBlocker(EntityUid ent, bool mob)
    {
        if (HasComp<GhostComponent>(ent) || HasComp<CargoTransitComponent>(ent) ||
            HasComp<ChasmFallingComponent>(ent))
            return false;
        if (mob)
            return true;
        return TryComp<PhysicsComponent>(ent, out var body) && body.CanCollide && body.Hard;
    }

    private static HashSet<EntityUid> OwnSet(Transfer transfer)
    {
        var own = new HashSet<EntityUid>();
        foreach (var cargo in transfer.Items)
        {
            own.Add(cargo.Item);
            if (cargo.Invoice is { } paper)
                own.Add(paper);
        }
        return own;
    }

    #region Sealing (anti-dupe)

    public void CollectTree(EntityUid uid, HashSet<EntityUid> into)
    {
        if (!into.Add(uid))
            return;
        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
            CollectTree(child, into);
    }

    public bool TreeUnchanged(EntityUid uid, HashSet<EntityUid> snapshot)
    {
        _treeScratch.Clear();
        if (TerminatingOrDeleted(uid))
            return false;
        CollectTree(uid, _treeScratch);
        return _treeScratch.SetEquals(snapshot);
    }

    private void AddCargo(Transfer transfer, EntityUid item, EntityUid? invoice, Vector2 start, double quoted)
    {
        _pulling.StopAllPulls(item);
        var collide = TryComp<PhysicsComponent>(item, out var body) && body.CanCollide;
        if (body != null)
        {
            _physics.SetLinearVelocity(item, Vector2.Zero, body: body);
            _physics.SetCanCollide(item, false, body: body);
        }
        EnsureComp<CargoTransitComponent>(item);
        var snapshot = new HashSet<EntityUid>();
        CollectTree(item, snapshot);
        foreach (var ent in snapshot)
            EnsureComp<CargoSealedComponent>(ent);
        if (invoice is { } paper && !TerminatingOrDeleted(paper) && Transform(paper).ParentUid == transfer.Grid)
        {
            EnsureComp<CargoTransitComponent>(paper);
            EnsureComp<CargoSealedComponent>(paper);
        }
        transfer.Items.Add(new Cargo(item, invoice, start, collide, snapshot, quoted));
    }

    private void ReleaseCargo(Cargo cargo)
    {
        foreach (var ent in cargo.Snapshot)
        {
            if (!TerminatingOrDeleted(ent))
                RemComp<CargoSealedComponent>(ent);
        }
        if (!TerminatingOrDeleted(cargo.Item))
        {
            RemComp<CargoTransitComponent>(cargo.Item);
            RemComp<CargoSealedComponent>(cargo.Item);
            if (TryComp<PhysicsComponent>(cargo.Item, out var body))
                _physics.SetCanCollide(cargo.Item, cargo.CanCollide, body: body);
        }
        if (cargo.Invoice is { } paper && !TerminatingOrDeleted(paper))
        {
            RemComp<CargoTransitComponent>(paper);
            RemComp<CargoSealedComponent>(paper);
        }
    }

    #endregion

    private void Begin(EntityUid uid, CargoDockComponent dock, Transfer transfer)
    {
        _transfers.Add(uid, transfer);
        SetState(uid, dock, CargoState.Preparing);
        SetMachinePhase(uid, dock, CargoPhase.Warning);
        _audio.PlayPvs(StartSound, uid);
    }

    private static float DepthAt(bool export, float elapsed)
    {
        var move = Ease((elapsed - OutStart) / (OutEnd - OutStart));
        return export ? move : 1f - move;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _remove.Clear();
        foreach (var (uid, transfer) in _transfers)
        {
            if (!TryComp<CargoDockComponent>(uid, out var dock) ||
                TerminatingOrDeleted(transfer.Grid) || !Usable(uid, dock, out var grid) ||
                grid != transfer.Grid || _station.GetOwningStation(uid) != transfer.Station ||
                Vector2.DistanceSquared(Transform(uid).LocalPosition, transfer.DockPosition) > 0.01f)
            {
                Abort(transfer);
                _remove.Add(uid);
                continue;
            }

            if (!transfer.CargoReleased)
            {
                for (var i = transfer.Items.Count - 1; i >= 0; i--)
                {
                    var cargo = transfer.Items[i];
                    if (CargoStillControlled(transfer, cargo))
                        continue;
                    ReleaseCargo(cargo);
                    transfer.Items.RemoveAt(i);
                }
                if (transfer.Items.Count == 0 && transfer.Export)
                {
                    _remove.Add(uid);
                    continue;
                }
            }

            transfer.HealthCheck -= frameTime;
            transfer.BuzzCooldown -= frameTime;
            if (transfer.HealthCheck <= 0)
            {
                transfer.HealthCheck = 0.1f;

                var shaftOpen = transfer.Elapsed < (transfer.Export ? InEnd : Release);
                var own = OwnSet(transfer);

                if (dock.FallThrough && PlatformDepth(transfer.Export, transfer.Elapsed) >= HoleDepth)
                    DropIntoShaft(uid, grid, transfer.Center, PadHalf(uid), own);
                if (shaftOpen && !LaneClear(uid, grid, transfer.Center, PadHalf(uid), own))
                {
                    if (dock.State != CargoState.Blocked && transfer.BuzzCooldown <= 0)
                    {
                        _audio.PlayPvs(BlockedSound, uid);
                        transfer.BuzzCooldown = BlockedBuzzCooldown;
                    }
                    SetState(uid, dock, CargoState.Blocked);
                }
                else if (dock.State == CargoState.Blocked)
                    SetState(uid, dock, transfer.Elapsed < OutStart ? CargoState.Preparing : CargoState.Moving);
            }
            if (dock.State == CargoState.Blocked)
            {
                transfer.BlockedTime += frameTime;
                if (transfer.BlockedTime > 15f)
                {
                    Abort(transfer);
                    _remove.Add(uid);
                }
                continue;
            }

            var previous = transfer.Elapsed;
            transfer.Elapsed = MathF.Min(Complete, transfer.Elapsed + frameTime);
            PlayMilestoneSounds(uid, transfer.Export, previous, transfer.Elapsed);
            SetState(uid, dock, transfer.Elapsed < OutStart ? CargoState.Preparing : CargoState.Moving);
            SetMachinePhase(uid, dock, PhaseAt(transfer.Elapsed));

            if (!transfer.CargoReleased)
            {
                var scale = 1f - (1f - BottomScale) * DepthAt(transfer.Export, transfer.Elapsed);
                foreach (var cargo in transfer.Items)
                {
                    var position = transfer.Center + (cargo.Start - transfer.Center) * scale;
                    if (Vector2.DistanceSquared(position, cargo.LastPosition) > 0.00001f)
                        _transform.SetCoordinates(cargo.Item, new EntityCoordinates(grid, position));
                    cargo.LastPosition = position;
                    if (TryComp<PhysicsComponent>(cargo.Item, out var body))
                        _physics.SetLinearVelocity(cargo.Item, Vector2.Zero, body: body);
                    if (cargo.Invoice is { } paper && !TerminatingOrDeleted(paper) && Transform(paper).ParentUid == grid)
                        _transform.SetCoordinates(paper, new EntityCoordinates(grid, position));
                }
                if (!transfer.Export && transfer.Elapsed >= Release)
                {
                    foreach (var cargo in transfer.Items)
                        ReleaseCargo(cargo);
                    transfer.CargoReleased = true;
                    _audio.PlayPvs(ArrivalSound, uid);
                }
            }
            if (transfer.Elapsed < Complete)
                continue;
            if (transfer.Export)
                FinishExport(uid, transfer);
            _remove.Add(uid);
        }
        foreach (var uid in _remove)
        {
            _transfers.Remove(uid);
            if (TryComp<CargoDockComponent>(uid, out var dock))
            {
                dock.CooldownLeft = MathF.Max(0.2f, dock.Cooldown);
                SetState(uid, dock, CargoState.Idle);
                SetMachinePhase(uid, dock, CargoPhase.Idle);
            }
        }
        var docks = EntityQueryEnumerator<CargoDockComponent>();
        while (docks.MoveNext(out _, out var dock))
            dock.CooldownLeft = MathF.Max(0, dock.CooldownLeft - frameTime);
        _scan -= frameTime;
        if (_scan > 0)
            return;
        _scan = 0.25f;

        var idle = EntityQueryEnumerator<CargoDockComponent>();
        while (idle.MoveNext(out var idleUid, out var idleDock))
        {
            if (idleDock.State == CargoState.Blocked && !_transfers.ContainsKey(idleUid))
                SetState(idleUid, idleDock, CargoState.Idle);
        }
        DispatchImports();
    }

    private void FinishExport(EntityUid uid, Transfer transfer)
    {
        var lots = transfer.Items
            .Select(c => new CargoSystem.LiftExportLot(c.Item, c.Snapshot, c.Quoted))
            .ToList();
        var sold = new HashSet<EntityUid>();
        var paid = _cargo.LiftCommitExports(lots, transfer.Station, sold);
        foreach (var cargo in transfer.Items)
        {
            if (sold.Contains(cargo.Item))
                continue;

            ReturnToPad(transfer, cargo);
            ReleaseCargo(cargo);
        }
        if (paid > 0)
            _audio.PlayPvs(PaidSound, uid);
    }

    private static float Ease(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private bool CargoStillControlled(Transfer transfer, Cargo cargo)
    {
        if (TerminatingOrDeleted(cargo.Item))
            return false;
        var xform = Transform(cargo.Item);
        return xform.ParentUid == transfer.Grid && !xform.Anchored &&
            Vector2.DistanceSquared(xform.LocalPosition, cargo.LastPosition) <= DriftSquared;
    }

    private static bool Crossed(float previous, float now, float mark) => previous < mark && now >= mark;

    private void PlayMilestoneSounds(EntityUid uid, bool export, float previous, float now)
    {
        if (Crossed(previous, now, OpenStart))
        {
            _audio.PlayPvs(UnlockSound, uid);
            if (!export)
                _audio.PlayPvs(HydraulicSound, uid);
        }
        if (export)
        {
            if (Crossed(previous, now, OutStart))
            {
                _audio.PlayPvs(DriveDownSound, uid);
                _audio.PlayPvs(HydraulicSound, uid);
            }
            if (Crossed(previous, now, OutEnd))
                _audio.PlayPvs(BottomSound, uid);
            if (Crossed(previous, now, Release))
            {
                _audio.PlayPvs(DriveUpSound, uid);
                _audio.PlayPvs(HydraulicSound, uid);
            }
            if (Crossed(previous, now, InEnd))
                _audio.PlayPvs(LockSound, uid);
            return;
        }
        if (Crossed(previous, now, OutStart))
        {
            _audio.PlayPvs(BottomSound, uid);
            _audio.PlayPvs(DriveUpSound, uid);
            _audio.PlayPvs(HydraulicSound, uid);
        }
        if (Crossed(previous, now, OutEnd))
            _audio.PlayPvs(LockSound, uid);
    }

    public static float PlatformDepth(bool export, float elapsed)
    {
        if (export)
        {
            if (elapsed < OutStart)
                return 0f;
            if (elapsed < OutEnd)
                return Ease((elapsed - OutStart) / (OutEnd - OutStart));
            if (elapsed < Release)
                return 1f;
            if (elapsed < InEnd)
                return 1f - Ease((elapsed - Release) / (InEnd - Release));
            return 0f;
        }
        if (elapsed < OpenStart)
            return 0f;
        if (elapsed < OutStart)
            return Ease((elapsed - OpenStart) / (OutStart - OpenStart));
        if (elapsed < OutEnd)
            return 1f - Ease((elapsed - OutStart) / (OutEnd - OutStart));
        return 0f;
    }

    private void DropIntoShaft(EntityUid uid, EntityUid grid, Vector2 center, float half, IReadOnlySet<EntityUid> own)
    {
        foreach (var ent in PadEntities(uid, grid, center, half).ToList())
        {
            if (own.Contains(ent) || !HasComp<MobStateComponent>(ent) || HasComp<GhostComponent>(ent) ||
                HasComp<ChasmFallingComponent>(ent) || _gravity.IsWeightless(ent))
                continue;
            _pulling.StopAllPulls(ent);
            if (TryComp<PhysicsComponent>(ent, out var body))
                _physics.SetLinearVelocity(ent, Vector2.Zero, body: body);
            var falling = AddComp<ChasmFallingComponent>(ent);
            falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
            _blocker.UpdateCanMove(ent);
            _audio.PlayPvs(FallSound, ent);
            Log.Info($"{ToPrettyString(ent)} fell into the open shaft of cargo lift {ToPrettyString(uid)}.");
        }
    }

    private void DispatchImports()
    {
        for (var index = _deliveries.Count - 1; index >= 0; index--)
        {
            var delivery = _deliveries[index];
            if (TerminatingOrDeleted(delivery.Station))
            {
                _deliveries.RemoveAt(index);
                continue;
            }
            if (delivery.Remaining > 0 && _timing.CurTime - delivery.AcceptedAt > TimeSpan.FromMinutes(3))
            {
                _cargo.LiftRefund(delivery.Station, delivery.Order, delivery.Remaining);
                delivery.Remaining = 0;
            }
            if (delivery.Remaining == 0)
                _deliveries.RemoveAt(index);
        }
        var query = EntityQueryEnumerator<CargoDockComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dock, out var xform))
        {
            if (dock.Mode != CargoDockMode.Import || _transfers.ContainsKey(uid) || dock.CooldownLeft > 0 ||
                !Usable(uid, dock, out var grid) || _station.GetOwningStation(uid) is not { } station)
                continue;
            if (!_deliveries.Any(d => d.Station == station && d.Remaining > 0))
                continue;
            var center = PadCenter(uid);

            if (!LaneClear(uid, grid, center, PadHalf(uid)))
            {
                SetState(uid, dock, CargoState.Blocked);
                continue;
            }
            var transfer = new Transfer(grid, center, xform.LocalPosition, station, false);
            var slots = ImportSlots(uid, center);
            var batch = Math.Min(Capacity(uid, dock), slots.Count);

            foreach (var delivery in _deliveries.Where(d => d.Station == station))
            {
                while (delivery.Remaining > 0 && transfer.Items.Count < batch)
                {
                    var slot = slots[transfer.Items.Count];

                    var bottom = center + (slot - center) * BottomScale;
                    var (item, invoice) = _cargo.LiftSpawnDelivery(delivery.Order, new EntityCoordinates(grid, bottom));

                    delivery.Remaining--;
                    delivery.Order.NumDispatched++;
                    AddCargo(transfer, item, invoice, slot, 0);
                    transfer.Items[^1].LastPosition = bottom;
                }
                if (transfer.Items.Count >= batch)
                    break;
            }
            if (transfer.Items.Count > 0)
                Begin(uid, dock, transfer);
        }
    }

    private void ReturnToPad(Transfer transfer, Cargo cargo)
    {
        if (TerminatingOrDeleted(transfer.Grid) || TerminatingOrDeleted(cargo.Item) ||
            Transform(cargo.Item).ParentUid != transfer.Grid ||
            Vector2.DistanceSquared(Transform(cargo.Item).LocalPosition, cargo.LastPosition) > DriftSquared)
            return;
        _transform.SetCoordinates(cargo.Item, new EntityCoordinates(transfer.Grid, cargo.Start));
        if (cargo.Invoice is { } paper && !TerminatingOrDeleted(paper) && Transform(paper).ParentUid == transfer.Grid)
            _transform.SetCoordinates(paper, new EntityCoordinates(transfer.Grid, cargo.Start));
    }

    private void Abort(Transfer transfer)
    {
        foreach (var cargo in transfer.Items)
        {
            if (!transfer.CargoReleased)
                ReturnToPad(transfer, cargo);
            ReleaseCargo(cargo);
        }
    }

    private void SetState(EntityUid uid, CargoDockComponent dock, CargoState state)
    {
        if (dock.State == state && _appearance.TryGetData<CargoState>(uid, CargoVisuals.State, out var old) && old == state)
            return;
        dock.State = state;
        var paused = state == CargoState.Blocked;
        _appearance.SetData(uid, CargoVisuals.State, state);
        _appearance.SetData(uid, CargoVisuals.Export, dock.Mode == CargoDockMode.Export);
        _appearance.SetData(uid, CargoVisuals.Paused, paused);
    }

    private void SetMachinePhase(EntityUid uid, CargoDockComponent dock, CargoPhase phase, bool force = false)
    {
        if (!force && dock.VisualPhase == phase)
            return;
        dock.VisualPhase = phase;
        var export = dock.Mode == CargoDockMode.Export;
        var paused = dock.State == CargoState.Blocked;
        _appearance.SetData(uid, CargoVisuals.Phase, phase);
        _appearance.SetData(uid, CargoVisuals.Export, export);
        _appearance.SetData(uid, CargoVisuals.Paused, paused);
    }
}
