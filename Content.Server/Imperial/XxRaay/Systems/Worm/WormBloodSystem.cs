using Content.Shared.Alert;
using Content.Shared.Eye;
using Content.Shared.Imperial.XxRaay;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Helpers;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormBloodSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly Robust.Shared.Timing.IGameTiming _timing = default!;

    private EntityQuery<WormBloodComponent> _bloodQuery;
    private TimeSpan _accumulator = TimeSpan.Zero;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        base.Initialize();
        _bloodQuery = GetEntityQuery<WormBloodComponent>();

        SubscribeLocalEvent<WormBloodComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WormBloodComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WormBloodComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, ComponentStartup>(OnCorpseOccupiedStartup);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, ComponentShutdown>(OnCorpseOccupiedShutdown);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, GetVisMaskEvent>(OnCorpseOccupiedGetVisMask);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _accumulator)
            return;

        _accumulator = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<WormBloodComponent>();
        while (query.MoveNext(out var uid, out var blood))
        {
            var severity = WormBloodHelper.GetSeverity(blood.Blood);
            if (blood.LastAlertSeverity == severity)
                continue;

            blood.LastAlertSeverity = severity;
            UpdateBloodAlert(uid, blood, severity);
        }
    }

    private void OnStartup(EntityUid uid, WormBloodComponent component, ComponentStartup args)
    {
        var severity = WormBloodHelper.GetSeverity(component.Blood);
        component.LastAlertSeverity = severity;
        UpdateBloodAlert(uid, component, severity);

        if (TryComp(uid, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((uid, eye));
    }

    private void OnShutdown(EntityUid uid, WormBloodComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, component.BloodAlert);

        if (TryComp(uid, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((uid, eye));
    }

    private void OnGetVisMask(Entity<WormBloodComponent> ent, ref GetVisMaskEvent args)
    {
        AddWormVisibilityLayers(ref args);
    }

    private void OnCorpseOccupiedStartup(Entity<WormCorpseOccupiedComponent> ent, ref ComponentStartup args)
    {
        if (TryComp(ent.Owner, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((ent.Owner, eye));
    }

    private void OnCorpseOccupiedShutdown(Entity<WormCorpseOccupiedComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp(ent.Owner, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((ent.Owner, eye));
    }

    private void OnCorpseOccupiedGetVisMask(Entity<WormCorpseOccupiedComponent> ent, ref GetVisMaskEvent args)
    {
        AddWormVisibilityLayers(ref args);
    }

    private static void AddWormVisibilityLayers(ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= XxRaayVisibilityLayers.SpiderVentMask;
        args.VisibilityMask |= XxRaayVisibilityLayers.DoorHideMask;
    }

    public void SetBlood(EntityUid uid, int value, WormBloodComponent? component = null)
    {
        if (!_bloodQuery.Resolve(uid, ref component))
            return;

        var clamped = Math.Clamp(value, 0, component.MaxBlood);
        if (component.Blood == clamped)
            return;

        component.Blood = clamped;
        Dirty(uid, component);

        var severity = WormBloodHelper.GetSeverity(component.Blood);
        component.LastAlertSeverity = severity;
        UpdateBloodAlert(uid, component, severity);
    }

    public void ModifyBlood(EntityUid uid, int delta, WormBloodComponent? component = null)
    {
        if (!_bloodQuery.Resolve(uid, ref component))
            return;

        SetBlood(uid, component.Blood + delta, component);
    }

    public int TryAddBlood(EntityUid uid, int amount, WormBloodComponent? component = null)
    {
        if (!_bloodQuery.Resolve(uid, ref component) || amount <= 0)
            return 0;

        var space = component.MaxBlood - component.Blood;
        var added = Math.Min(amount, space);
        if (added <= 0)
            return 0;

        SetBlood(uid, component.Blood + added, component);
        return added;
    }

    private void UpdateBloodAlert(EntityUid uid, WormBloodComponent component, short severity)
    {
        if (!HasComp<AlertsComponent>(uid))
            return;

        _alerts.ShowAlert(uid, component.BloodAlert, severity);
    }
}
