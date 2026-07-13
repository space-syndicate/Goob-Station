// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Lock;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.MiningCrate;

/// <summary>
/// Countdown unlock + green lock sprite blink.
/// Free unlock when no payment module claims <see cref="MiningCrateTryUnlockEvent"/> first.
/// Payment modules call <see cref="StartTimer"/> after a successful purchase.
/// </summary>
public sealed class MiningCrateUnlockTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MiningCrateSystem _crate = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        // After points unlock so free timer only runs when payment module is absent.
        SubscribeLocalEvent<MiningCrateUnlockTimerComponent, MiningCrateTryUnlockEvent>(OnTryUnlock);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<MiningCrateUnlockTimerComponent, MiningCrateComponent, LockComponent>();
        while (query.MoveNext(out var uid, out var timer, out var crate, out var lockComp))
        {
            if (crate.Unlocked || !timer.Started || timer.UnlockAt == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < timer.UnlockAt)
            {
                if (_crate.IsDevicePowered(uid) && _timing.CurTime >= timer.NextUnlockBlink)
                {
                    timer.NextUnlockBlink = _timing.CurTime + timer.UnlockBlinkInterval;
                    timer.UnlockBlinkShowUnlocked = !timer.UnlockBlinkShowUnlocked;
                    Dirty(uid, timer);
                    _crate.UpdateCrateVisuals(uid);
                }

                continue;
            }

            _crate.CompleteUnlock((uid, crate), lockComp);
        }
    }

    private void OnTryUnlock(Entity<MiningCrateUnlockTimerComponent> ent, ref MiningCrateTryUnlockEvent args)
    {
        if (args.Handled)
            return;

        // Payment modules (points, …) handle TryUnlock themselves and call StartTimer.
        if (HasComp<MiningCratePointsUnlockComponent>(ent))
            return;

        if (!TryComp<MiningCrateComponent>(ent, out var crate) || crate.Unlocked)
            return;

        if (ent.Comp.Started)
        {
            args.Handled = true;
            _crate.DenyFeedback(ent, args.User, _crate.GetStatusMessage(ent));
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        StartTimer(ent, ent.Comp, args.User, purchased: false);
    }

    /// <summary>
    /// Begins the unlock countdown (after free use or successful payment).
    /// </summary>
    public void StartTimer(
        EntityUid uid,
        MiningCrateUnlockTimerComponent timer,
        EntityUid user,
        bool purchased)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate) || crate.Unlocked)
            return;

        timer.Started = true;
        timer.UnlockAt = _timing.CurTime + timer.UnlockDelay;
        timer.UnlockBlinkShowUnlocked = true;
        timer.NextUnlockBlink = _timing.CurTime + timer.UnlockBlinkInterval;
        Dirty(uid, timer);

        _crate.SetDenySiren(uid, false);
        _crate.EnsurePhysicallyLocked(uid, user);
        _crate.UpdateCrateVisuals(uid);

        _audio.PlayPvs(crate.PurchaseSound, uid);
        _audio.PlayPvs(crate.LockSound, uid);

        var seconds = timer.UnlockDelay.TotalSeconds.ToString("0");
        var msg = purchased
            ? Loc.GetString("lavaland-mining-crate-purchased", ("seconds", seconds))
            : Loc.GetString("lavaland-mining-crate-unlock-started", ("seconds", seconds));
        _popup.PopupEntity(msg, uid, user);
    }
}
