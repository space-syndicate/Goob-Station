
using Content.Server.Imperial.SCP.NothingThere.Components;
using Content.Shared.Imperial.SCP.NothingThere.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;
namespace Content.Server.Imperial.SCP.NothingThere.Systems;

public sealed partial class ImperialNothingThereSystem
{
    #region Initialize
    private void InitializeArsenal()
    {
        SubscribeLocalEvent<ImperialNothingThereComponent, ImperialNothingThereGoodbyeEvent>(OnGoodbyeAction);
        SubscribeLocalEvent<ImperialNothingThereComponent, ImperialNothingThereGoodbyeDoAfterEvent>(OnGoodbyeDoAfter);
        SubscribeLocalEvent<ImperialNothingThereGoodbyeComponent, MeleeHitEvent>(OnGoodbyeAttack);
    }
    #endregion
    #region Item provision
    private void UpdateItemProvide()
    {
        var query = EntityQueryEnumerator<ImperialNothingThereComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NeedItems == false)
                continue;
            var hands = EnsureComp<HandsComponent>(uid);
            if (_hands.TryGetEmptyHand((uid, hands), out var emptyHand))
            {
                var hit = EntityUid.Invalid;
                if (comp.NeedGoodbye == true)
                {
                    hit = Spawn(comp.GoodbyeProto, Transform(uid).Coordinates);
                    comp.NeedGoodbye = false;
                }
                else
                {
                    hit = Spawn(comp.HitProto, Transform(uid).Coordinates);
                }
                if (!_hands.TryForcePickup(uid, hit, emptyHand, checkActionBlocker: false, handsComp: hands))
                {
                    QueueDel(hit);
                    return;
                }
                else
                    comp.NeedItems = false;
            }
        }
    }
    #endregion
    #region Goodbye Handle
    private void OnGoodbyeAction(Entity<ImperialNothingThereComponent> ent, ref ImperialNothingThereGoodbyeEvent args)
    {
        if (args.Handled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.GoodbyeDelay, new ImperialNothingThereGoodbyeDoAfterEvent(), ent)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            NeedHand = false
        };
        if (!TryComp<MobStateComponent>(args.Performer, out var mob))
            return;
        if (!TryComp<ImperialNothingThereComponent>(args.Performer, out var comp))
            return;
        if (_doAfterSystem.TryStartDoAfter(doAfterArgs))
        {
            args.Handled = true;
            _popupSystem.PopupEntity(Loc.GetString("nothing-there-goodbye-windup"), ent, PopupType.MediumCaution);
        }
        args.Handled = true;
    }
    private void OnGoodbyeDoAfter(Entity<ImperialNothingThereComponent> ent, ref ImperialNothingThereGoodbyeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var hands = EnsureComp<HandsComponent>(args.User);
        var handd = _hands.GetActiveHand((args.User, hands));
        var wpn = _hands.GetActiveItem((args.User, hands));
        var user = args.User;
        _hands.DoDrop((args.User, hands), handd!, false, false);
        QueueDel(wpn);
        ent.Comp.NeedGoodbye = true;
        ent.Comp.NeedItems = true;
    }

    private void OnGoodbyeAttack(EntityUid uid, ImperialNothingThereGoodbyeComponent comp, ref MeleeHitEvent args)
    {
        if (comp.Used || args.Handled)
            return;
        if (!TryComp<HandsComponent>(args.User, out var hands))
        args.Handled = true;
        _audio.PlayPvs(comp.EmpowerSound, args.User);
        comp.Used = true;
        var user = args.User;
        var handd = _hands.GetActiveHand((user, hands));
        Timer.Spawn(100, () =>
        {
            _hands.DoDrop((user, hands), handd!, false, false);
            if (Exists(uid))
                QueueDel(uid);
            if (TryComp<ImperialNothingThereComponent>(user, out var scp))
            {
                scp.NeedItems = true;
            }
        });
    }
    #endregion
}
