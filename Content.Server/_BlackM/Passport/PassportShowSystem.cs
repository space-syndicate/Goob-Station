using Content.Server.Administration.Managers;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared._BlackM.Passport;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._BlackM.Passport;

public sealed class PassportShowSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands     = default!;
    [Dependency] private readonly InventorySystem   _inventory = default!;
    [Dependency] private readonly PopupSystem       _popup     = default!;
    [Dependency] private readonly PassportSystem    _passport  = default!;
    [Dependency] private readonly IAdminManager     _admin     = default!;

    private const string PassportSlot = "passport";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetAdminVerbs);
    }

    private void OnGetVerbs(EntityUid target, HumanoidAppearanceComponent humanoid, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        if (user == target || !HasComp<ActorComponent>(target))
            return;

        if (!TryFindPassport(user, out _))
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("passport-verb-show"),
            Priority = 5,
            Act = () => ShowPassport(user, target),
        });
    }

    private void OnGetAdminVerbs(GetVerbsEvent<Verb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (user == target || !HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (!HasComp<GhostComponent>(user) || !TryComp<ActorComponent>(user, out var userActor))
            return;

        if (!_admin.HasAdminFlag(userActor.PlayerSession, AdminFlags.Admin))
            return;

        if (!TryFindPassportFor(target, out var passport))
            return;

        var session = userActor.PlayerSession;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("passport-verb-admin-view"),
            Category = VerbCategory.Admin,
            Priority = -10,
            Act = () =>
            {
                if (!TryComp<PassportComponent>(passport, out var comp) || Deleted(passport))
                    return;
                RaiseNetworkEvent(new PassportShownEvent(_passport.BuildState(comp)), session);
            },
        });
    }

    private bool TryFindPassportFor(EntityUid target, out EntityUid passport)
    {
        if (TryFindPassport(target, out passport))
            return true;

        var netTarget = GetNetEntity(target);
        var query = EntityQueryEnumerator<PassportComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsBound && comp.OwnerEntity == netTarget)
            {
                passport = uid;
                return true;
            }
        }

        passport = default;
        return false;
    }

    private void ShowPassport(EntityUid user, EntityUid target)
    {
        if (!TryFindPassport(user, out var passport) || !TryComp<PassportComponent>(passport, out var comp))
            return;

        if (!TryComp<ActorComponent>(target, out var actor))
            return;

        RaiseNetworkEvent(new PassportShownEvent(_passport.BuildState(comp)), actor.PlayerSession);

        var msg = Loc.GetString("passport-show-popup",
            ("user", Identity.Name(user, EntityManager)),
            ("target", Identity.Name(target, EntityManager)));

        _popup.PopupEntity(msg, user, PopupType.Small);
    }

    private bool TryFindPassport(EntityUid user, out EntityUid passport)
    {
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (IsFilledPassport(held))
            {
                passport = held;
                return true;
            }
        }

        if (_inventory.TryGetSlotEntity(user, PassportSlot, out var worn) && IsFilledPassport(worn.Value))
        {
            passport = worn.Value;
            return true;
        }

        passport = default;
        return false;
    }

    private bool IsFilledPassport(EntityUid uid)
        => TryComp<PassportComponent>(uid, out var comp) && comp.IsBound;
}
