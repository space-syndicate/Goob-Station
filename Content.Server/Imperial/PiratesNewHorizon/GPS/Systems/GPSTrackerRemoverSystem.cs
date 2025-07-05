
using Content.Server.Cargo.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Cargo.Events;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Server.Storage;
using Content.Shared.Storage;
using Content.Server.Cargo;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Systems;
using Content.Server.Imperial.PiratesNewHorizon.GPS.Components;
using Content.Shared.Imperial.PiratesNewHorizon.GPS.Events;
namespace Content.Server.Imperial.PiratesNewHorizon.GPS.Systems;
public sealed class GPSTrackerRemoverSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly CargoSystem _bountySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<GPSTrackerRemoverComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GPSTrackerRemoverComponent, GPSTrackerRemoveDoAfterEvent>(DoAfterInteract);
    }

    private void DoAfterInteract(Entity<GPSTrackerRemoverComponent> entity, ref GPSTrackerRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;
        if (!TryComp(entity, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((entity, useDelay)))
            return;
        if (!TryComp(args.Target, out GPSTrackerPriceComponent? gpsTracker) || gpsTracker.GPSTrackerInstalled == false)
            return;
        if (!TryComp(entity, out GPSTrackerRemoverComponent? gpsTrackerRemover))
            return;
        var target = args.Target ?? args.User; /// Yall might be asking wtf is this piece of shit, but this actually is used to avoid fucking nullable-to-non-nullable conversion error
        gpsTracker.GPSTrackerInstalled = false;
        _popupSystem.PopupEntity(Loc.GetString("gps-removed-result"), args.User, args.User);
        _audioSystem.PlayPvs(gpsTrackerRemover.UseSound, entity, AudioParams.Default.WithVolume(-1f));
        var coords = Transform(target).Coordinates;
        foreach (var entry in gpsTrackerRemover.ItemsToSpawn)
        {
            Spawn(entry.PrototypeId, coords);
        }
        _useDelay.TryResetDelay((entity, useDelay));
        args.Handled = true;
    }
    private void OnAfterInteract(EntityUid uid, GPSTrackerRemoverComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;
        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!TryComp(args.Target, out GPSTrackerPriceComponent? gpsTracker) || gpsTracker.GPSTrackerInstalled == false)
        {
            _popupSystem.PopupEntity(Loc.GetString("gps-missing-result"), args.User, args.User);
            return;
        }
        var doAfterArgs = new DoAfterArgs(EntityManager,
        args.User,
        TimeSpan.FromSeconds(component.Delay),
        new GPSTrackerRemoveDoAfterEvent(),
        eventTarget: uid,
        target: args.Target,
        used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        _popupSystem.PopupEntity(Loc.GetString("gps-found-result"), args.User, args.User);
        args.Handled = true;
    }
}
