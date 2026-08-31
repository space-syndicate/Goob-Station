// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Sandevistan;
using Content.Goobstation.Shared.Sprinting;
using Content.Server.Stunnable;
using Content.Shared.CombatMode;
using Robust.Shared.Physics.Events;

namespace Content.Goobstation.Server.Sprinting;

public sealed class SprintingSystem : SharedSprintingSystem
{

    [Dependency] private readonly StunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        // SubscribeLocalEvent<SprinterComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, SprinterComponent sprinter, ref StartCollideEvent args)
    {
        // Collision of two sprinting entities (knockdown of both) is disabled.
        // var otherUid = args.OtherEntity;
        //
        // if (uid.Id < otherUid.Id)
        //     return;
        //
        // if (!sprinter.IsSprinting)
        // {
        //     return;
        // }
        //
        // if (!TryComp(otherUid, out SprinterComponent? otherSprinter)
        //     || !otherSprinter.IsSprinting
        //     || !HasComp<ActiveSandevistanUserComponent>(otherUid))
        // {
        //     return;
        // }
        //
        // _stunSystem.TryKnockdown(uid, sprinter.KnockdownDurationOnInterrupt, false, true);
        // _stunSystem.TryKnockdown(otherUid,
        //     otherSprinter.KnockdownDurationOnInterrupt,
        //     false,
        //     true);
    }
}
//Было откачено ибо оно не где не использовалось кроме санда + оно отключенно..ыу