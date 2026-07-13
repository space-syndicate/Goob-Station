// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.MiningCrate;

public abstract class SharedMiningCrateSecuritySystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MiningCrateSecurityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MiningCrateSecurityComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
    }

    private void OnExamined(Entity<MiningCrateSecurityComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Detonating)
        {
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-security-examine-detonating"));
            return;
        }

        if (ent.Comp.Armed)
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-security-examine-armed"));
        else
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-security-examine-disarmed"));

        if (ent.Comp.LockWireCut)
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-security-examine-lock-wire"));
    }

    private void OnLockToggleAttempt(Entity<MiningCrateSecurityComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!ent.Comp.LockWireCut)
            return;

        args.Cancelled = true;
        if (!args.Silent)
            Popup.PopupClient(Loc.GetString("lavaland-mining-crate-security-lock-wire-blocked"), ent, args.User);
    }
}
