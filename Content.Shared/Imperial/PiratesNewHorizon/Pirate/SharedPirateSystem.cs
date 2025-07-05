using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Imperial.PiratesNewHorizon.StatusIcons;
using Content.Shared.Stunnable;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Content.Shared.Antag;
namespace Content.Shared.Imperial.PiratesNewHorizon.Pirate;

public abstract class SharedPirateSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateComponent, ComponentGetStateAttemptEvent>(OnPirateCompGetStateAttempt);
        SubscribeLocalEvent<PirateCaptainComponent, ComponentGetStateAttemptEvent>(OnPirateCompGetStateAttempt);
        SubscribeLocalEvent<PirateComponent, ComponentStartup>(DirtyPirateComps);
        SubscribeLocalEvent<PirateCaptainComponent, ComponentStartup>(DirtyPirateComps);
        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentStartup>(DirtyPirateComps);
    }


    private void OnPirateCompGetStateAttempt(EntityUid uid, PirateCaptainComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }


    private void OnPirateCompGetStateAttempt(EntityUid uid, PirateComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }
    private bool CanGetState(ICommonSession? player)
    {
        if (player?.AttachedEntity is not {} uid)
            return true;

        if (HasComp<PirateComponent>(uid) || HasComp<PirateCaptainComponent>(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }
    private void DirtyPirateComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var pirateComps = AllEntityQuery<PirateComponent>();
        while (pirateComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var piratecapComps = AllEntityQuery<PirateCaptainComponent>();
        while (piratecapComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }
}
