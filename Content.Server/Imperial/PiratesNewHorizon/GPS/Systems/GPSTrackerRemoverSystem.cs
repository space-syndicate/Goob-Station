
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
using Content.Shared.Body.Components;
using Content.Shared.Hands.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Hands.Systems;
namespace Content.Server.Imperial.PiratesNewHorizon.GPS.Systems;

public sealed class GPSTrackerRemoverSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly CargoSystem _bountySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<GPSTrackerRemoverComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GPSTrackerRemoverComponent, GPSTrackerRemoveDoAfterEvent>(DoAfterInteract);
        SubscribeLocalEvent<GPSTrackerPriceComponent, GetVerbsEvent<AlternativeVerb>>(AddGPSVerb);
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
    private void AddGPSVerb(Entity<GPSTrackerPriceComponent> entity, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (entity.Owner == ev.User ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            !TryComp<BodyComponent>(ev.User, out var body) ||
            !TryComp<HandsComponent>(ev.User, out var hands) ||
            _handsSystem.TryGetActiveItem(ev.User, out var activeItem) ||
            !TryComp<GPSTrackerRemoverComponent>(activeItem, out var remover))
            return;
        var user = ev.User;
        var target = ev.Target;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                if (!TryComp(activeItem, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((activeItem.Value, useDelay)))
                    return;
                if (!TryComp(target, out GPSTrackerPriceComponent? gpsTracker) || gpsTracker.GPSTrackerInstalled == false)
                {
                    _popupSystem.PopupEntity(Loc.GetString("gps-missing-result"), user, user);
                    return;
                }
                var doAfterArgs = new DoAfterArgs(EntityManager,
                user,
                TimeSpan.FromSeconds(remover.Delay),
                new GPSTrackerRemoveDoAfterEvent(),
                eventTarget: activeItem,
                target: target,
                used: activeItem)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    MovementThreshold = 0.01f,
                    DistanceThreshold = 1.0f,
                    NeedHand = true,
                };
                _doAfter.TryStartDoAfter(doAfterArgs);
                _popupSystem.PopupEntity(Loc.GetString("gps-found-result"), user, user);
            },
            Text = Loc.GetString("gps-system-verb-remove"),
            Priority = -1
        };

        ev.Verbs.Add(verb);
    }
}
