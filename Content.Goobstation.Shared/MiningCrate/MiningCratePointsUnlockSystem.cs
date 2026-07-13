// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._DV.Salvage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.MiningCrate;

/// <summary>
/// Unlocks a mining crate by spending mining points, then starts <see cref="MiningCrateUnlockTimerComponent"/>.
/// </summary>
public sealed class MiningCratePointsUnlockSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MiningCrateSystem _crate = default!;
    [Dependency] private readonly MiningCrateUnlockTimerSystem _timer = default!;
    [Dependency] private readonly MiningPointsSystem _miningPoints = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MiningCratePointsUnlockComponent, MiningCrateTryUnlockEvent>(OnTryUnlock);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<MiningCratePointsUnlockComponent, MiningCrateComponent>();
        while (query.MoveNext(out var uid, out var points, out _))
        {
            if (points.DeniedUntil == TimeSpan.Zero || _timing.CurTime < points.DeniedUntil)
                continue;

            points.DeniedUntil = TimeSpan.Zero;
            Dirty(uid, points);
            _crate.SetDenySiren(uid, false);
            if (TryComp<MiningCrateComponent>(uid, out var crate))
                _audio.PlayPvs(crate.UnlockSound, uid);
        }
    }

    private void OnTryUnlock(Entity<MiningCratePointsUnlockComponent> ent, ref MiningCrateTryUnlockEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MiningCrateComponent>(ent, out var crate) || crate.Unlocked)
            return;

        if (!TryComp<MiningCrateUnlockTimerComponent>(ent, out var timer))
        {
            // Misconfigured prototype: points without a timer.
            args.Handled = true;
            _crate.DenyFeedback(ent, args.User, Loc.GetString("lavaland-mining-crate-locked"));
            return;
        }

        if (timer.Started)
        {
            args.Handled = true;
            _crate.DenyFeedback(ent, args.User, _crate.GetStatusMessage(ent));
            return;
        }

        if (IsDenyLockedOut(ent.Comp))
        {
            args.Handled = true;
            _crate.DenyFeedback(ent, args.User, _crate.GetStatusMessage(ent));
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (_miningPoints.GetPointComp(args.User) is not { } pointsHolder)
        {
            StartDenyLockout(ent, crate, args.User, Loc.GetString("lavaland-mining-crate-no-id"));
            TryShockOnFail(ent, crate, args.User);
            return;
        }

        var available = pointsHolder.Comp?.Points ?? 0;
        if (available < ent.Comp.Cost || !_miningPoints.RemovePoints(pointsHolder, ent.Comp.Cost))
        {
            StartDenyLockout(ent,
                crate,
                args.User,
                Loc.GetString("lavaland-mining-crate-not-enough-points", ("cost", ent.Comp.Cost)));
            TryShockOnFail(ent, crate, args.User);
            return;
        }

        ent.Comp.DeniedUntil = TimeSpan.Zero;
        Dirty(ent);
        _timer.StartTimer(ent.Owner, timer, args.User, purchased: true);
    }

    private void StartDenyLockout(
        Entity<MiningCratePointsUnlockComponent> ent,
        MiningCrateComponent crate,
        EntityUid user,
        string reasonMessage)
    {
        _crate.EnsurePhysicallyLocked(ent, user);

        if (ent.Comp.EnableDenyLockout)
        {
            ent.Comp.DeniedUntil = _timing.CurTime + ent.Comp.DenyLockoutDuration;
            Dirty(ent);

            if (_crate.IsDevicePowered(ent))
                _crate.SetDenySiren(ent, true);

            _audio.PlayPvs(crate.DenySound, ent);
            _audio.PlayPvs(crate.LockSound, ent);

            var remaining = ent.Comp.DenyLockoutDuration.TotalSeconds.ToString("0");
            _popup.PopupEntity(reasonMessage, ent, user);
            _popup.PopupEntity(
                Loc.GetString("lavaland-mining-crate-denied-lockout", ("seconds", remaining)),
                ent,
                user);
            return;
        }

        _crate.DenyFeedback(ent, user, reasonMessage);
    }

    private void TryShockOnFail(
        Entity<MiningCratePointsUnlockComponent> ent,
        MiningCrateComponent crate,
        EntityUid user)
    {
        if (ent.Comp.ShockOnFailChance <= 0f)
            return;

        if (!_random.Prob(ent.Comp.ShockOnFailChance))
            return;

        _electrocution.TryDoElectrocution(
            user,
            ent,
            ent.Comp.ShockDamage,
            ent.Comp.ShockDuration,
            refresh: true,
            siemensCoefficient: 1f,
            ignoreInsulation: false);

        _audio.PlayPvs(crate.ShockSound, ent);
        _popup.PopupEntity(Loc.GetString("lavaland-mining-crate-shocked"), ent, user);

        var ev = new MiningCrateForcePowerOffEvent();
        RaiseLocalEvent(ent, ref ev);
        if (!ev.Handled)
            _power.SetPowerDisabled(ent, true);

        _crate.SetDenySiren(ent, false);
        _crate.UpdateCrateVisuals(ent);
    }

    private bool IsDenyLockedOut(MiningCratePointsUnlockComponent points)
    {
        return points.DeniedUntil != TimeSpan.Zero && _timing.CurTime < points.DeniedUntil;
    }
}
