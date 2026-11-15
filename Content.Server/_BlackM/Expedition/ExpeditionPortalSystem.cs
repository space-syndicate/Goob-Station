using System.Numerics;
using Content.Server.Salvage.Expeditions;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.Expedition;
using Content.Shared.Audio;
using Content.Shared.Localizations;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.Expedition;

[ByRefEvent]
public record struct ExpeditionPortalEnteredEvent(EntityUid MapUid);

public sealed class ExpeditionPortalSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public const string ExitProto = "ExpeditionPortalExit";

    private static readonly TimeSpan TeleportCooldown = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CloseBefore = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WarnPlayersBefore = TimeSpan.FromSeconds(30);

    private static readonly int[] NotifyAt = { 300, 120, 60, 30, 10 };

    private readonly Dictionary<EntityUid, TimeSpan> _cooldowns = new();
    private readonly HashSet<(EntityUid, int)> _notified = new();
    private readonly HashSet<EntityUid> _warned = new();
    private TimeSpan _nextCheck;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExpeditionPortalComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ExpeditionPortalComponent, ComponentShutdown>(OnShutdown);
    }

    public EntityUid? FindEntrance(EntityUid station)
    {
        var query = EntityQueryEnumerator<ExpeditionPortalComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var portal, out var xform))
        {
            if (portal.IsExit || portal.Active || portal.Map != null)
                continue;

            if (_station.GetOwningStation(uid, xform) != station)
                continue;

            return uid;
        }

        return null;
    }

    public void WarnNoPortal(EntityUid console)
    {
        _popup.PopupEntity(Loc.GetString("expedition-portal-no-station-portal"), console, PopupType.MediumCaution);
    }

    public void LinkToMission(EntityUid entrance, EntityUid mapUid, MapGridComponent grid)
    {
        if (!TryComp<ExpeditionPortalComponent>(entrance, out var entranceComp))
            return;

        var exitCoords = _map.GridTileToLocal(mapUid, grid, Vector2i.Zero);
        var exit = Spawn(ExitProto, exitCoords);
        var exitComp = Comp<ExpeditionPortalComponent>(exit);
        exitComp.IsExit = true;

        var now = _timing.CurTime;
        TimeSpan? closes = null;
        if (TryComp<SalvageExpeditionComponent>(mapUid, out var expedition))
            closes = expedition.EndTime - CloseBefore;

        foreach (var (uid, comp, other) in new[] { (entrance, entranceComp, exit), (exit, exitComp, entrance) })
        {
            comp.Linked = other;
            comp.Map = mapUid;
            comp.OpenedAt = now;
            comp.ClosesAt = closes;
            comp.Active = true;
            Dirty(uid, comp);
            ApplyState(uid, true);
        }

        var left = closes is { } c ? ExpeditionPortalExamineSystem.FormatTime(c - now) : "?";
        _popup.PopupEntity(Loc.GetString("expedition-portal-opened", ("time", left)), entrance, PopupType.Large);
    }

    private void OnStartCollide(Entity<ExpeditionPortalComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.Active || ent.Comp.Linked is not { } target || TerminatingOrDeleted(target))
            return;

        var other = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(other, out var phys) || phys.BodyType == BodyType.Static)
            return;

        if (_cooldowns.TryGetValue(other, out var until) && until > _timing.CurTime)
            return;

        var firstEntry = false;
        EntityUid? expeditionMap = null;

        if (!ent.Comp.IsExit && ent.Comp.Map is { } mapUid)
        {
            expeditionMap = mapUid;

            if (_map.IsPaused(mapUid))
            {
                if (!HasComp<MobStateComponent>(other))
                    return;

                _map.SetPaused(mapUid, false);
                firstEntry = true;
            }
        }

        _cooldowns[other] = _timing.CurTime + TeleportCooldown;

        _audio.PlayPvs(ent.Comp.EnterSound, ent.Owner);
        _xform.SetCoordinates(other, Transform(target).Coordinates);

        if (TryComp<ExpeditionPortalComponent>(target, out var targetComp))
            _audio.PlayPvs(targetComp.ExitSound, target);

        if (expeditionMap is not { } map)
            return;

            if (HasComp<MobStateComponent>(other)) // personal objectives
                EntityManager.System<Content.Server._BlackM.PersonalObjectives.PersonalObjectivesSystem>().AddUniqueProgress(other, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.XenTrips, map);

        if (firstEntry)
        {
            var ev = new ExpeditionPortalEnteredEvent(map);
            RaiseLocalEvent(map, ref ev);
        }

        if (HasComp<MobStateComponent>(other) &&
            TryComp<SalvageExpeditionComponent>(map, out var expedition) &&
            expedition.DungeonLocation != Vector2.Zero)
        {
            var dir = ContentLocalizationManager.FormatDirection(expedition.DungeonLocation.GetDir()).ToLower();
            _popup.PopupEntity(Loc.GetString("salvage-expedition-announcement-dungeon", ("direction", dir)),
                other, other, PopupType.Large);
        }
    }

    private void ApplyState(EntityUid uid, bool active)
    {
        _appearance.SetData(uid, ExpeditionPortalVisuals.State,
            active ? ExpeditionPortalState.On : ExpeditionPortalState.Off);
        _ambient.SetAmbience(uid, active);
        _light.SetEnabled(uid, active);
    }

    private void OnShutdown(Entity<ExpeditionPortalComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Linked is not { } other || TerminatingOrDeleted(other))
            return;

        if (ent.Comp.IsExit)
            ResetEntrance(other);
        else
            QueueDel(other);
    }

    private void ResetEntrance(EntityUid entrance)
    {
        if (!TryComp<ExpeditionPortalComponent>(entrance, out var comp) || (!comp.Active && comp.Map == null))
            return;

        if (LifeStage(entrance) >= EntityLifeStage.Terminating)
            return;

        comp.Active = false;
        comp.Linked = null;
        comp.Map = null;
        comp.OpenedAt = null;
        comp.ClosesAt = null;
        Dirty(entrance, comp);
        ApplyState(entrance, false);
        _notified.RemoveWhere(x => x.Item1 == entrance);

        _popup.PopupEntity(Loc.GetString("expedition-portal-closed"), entrance, PopupType.Large);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;
        _nextCheck = _timing.CurTime + TimeSpan.FromSeconds(1);

        if (_cooldowns.Count > 0)
        {
            var expired = new List<EntityUid>();
            foreach (var (e, t) in _cooldowns)
            {
                if (t < _timing.CurTime || TerminatingOrDeleted(e))
                    expired.Add(e);
            }
            foreach (var e in expired)
                _cooldowns.Remove(e);
        }

        var query = EntityQueryEnumerator<ExpeditionPortalComponent>();
        while (query.MoveNext(out var uid, out var portal))
        {
            if (portal.IsExit || !portal.Active || portal.Map is not { } mapUid)
                continue;

            if (!TryComp<SalvageExpeditionComponent>(mapUid, out var expedition))
                continue;

            var left = expedition.EndTime - _timing.CurTime;

            int? hit = null;
            foreach (var t in NotifyAt)
            {
                if (left.TotalSeconds - CloseBefore.TotalSeconds <= t && _notified.Add((uid, t)))
                    hit = t;
            }

            if (hit != null)
            {
                var msg = Loc.GetString("expedition-portal-closing-in",
                    ("time", ExpeditionPortalExamineSystem.FormatTime(left - CloseBefore)));
                _popup.PopupEntity(msg, uid, PopupType.LargeCaution);
            }

            if (left <= WarnPlayersBefore && _warned.Add(uid))
                WarnPlayers(mapUid);

            if (left <= CloseBefore)
                ClosePortal(uid, portal);
        }
    }

    private void WarnPlayers(EntityUid mapUid)
    {
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var mob, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            _popup.PopupEntity(Loc.GetString("expedition-portal-closing"), mob, mob, PopupType.LargeCaution);
        }
    }

    private void ClosePortal(EntityUid entrance, ExpeditionPortalComponent portal)
    {
        if (portal.Linked is { } exit && TryComp<ExpeditionPortalComponent>(exit, out var exitComp))
        {
            exitComp.Active = false;
            exitComp.Linked = null;
            Dirty(exit, exitComp);
            ApplyState(exit, false);
        }

        _audio.PlayPvs(portal.ExitSound, entrance);
        _warned.Remove(entrance);
        ResetEntrance(entrance);
    }
}