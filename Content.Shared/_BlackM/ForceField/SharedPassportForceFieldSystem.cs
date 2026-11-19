using Content.Shared._BlackM.Passport;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._BlackM.ForceField;

public sealed class SharedPassportForceFieldSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PassportForceFieldComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(EntityUid uid, PassportForceFieldComponent comp, ref PreventCollideEvent args)
    {
        var target = args.OtherEntity;

        if (!HasComp<MobStateComponent>(target))
        {
            args.Cancelled = true;
            return;
        }

        var authorized = HasAuthorizedPassport(target);
        var now = _timing.CurTime;
        var canPlayFeedback = !comp.NextInteractionAllowed.TryGetValue(target, out var nextTime) || now >= nextTime;

        if (authorized)
            args.Cancelled = true;

        if (!canPlayFeedback)
            return;

        comp.NextInteractionAllowed[target] = now + comp.BumpCooldown;
        if (authorized)
        {
            _audio.PlayPredicted(comp.PassSound, uid, target);
            return;
        }

        _audio.PlayPredicted(comp.DenySound, uid, target);
        _popup.PopupClient(Loc.GetString("passport-forcefield-denied"), uid, target, PopupType.SmallCaution);
    }

    private bool HasAuthorizedPassport(EntityUid mob)
    {
        var slots = _inventory.GetSlotEnumerator(mob);
        while (slots.NextItem(out var item, out _))
        {
            if (IsPassportAuthorized(item))
                return true;
        }

        return false;
    }

    private bool IsPassportAuthorized(EntityUid? uid)
    {
        if (uid == null || !TryComp<PassportComponent>(uid.Value, out var passport))
            return false;

        return passport.Checked ||
               (passport.Stamp == PassportStampState.Approved && !passport.HasBureaucraticError);
    }
}
