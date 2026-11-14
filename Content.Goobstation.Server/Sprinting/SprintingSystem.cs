// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Sandevistan;
using Content.Goobstation.Shared.Sprinting;
using Content.Server.Stunnable;
using Content.Shared.CombatMode;
using Robust.Shared.Physics.Events;

namespace Content.Goobstation.Server.Sprinting;

public sealed partial class SprintingSystem : SharedSprintingSystem
{

    [Dependency] private StunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnCollide(EntityUid uid, SprinterComponent sprinter, ref StartCollideEvent args)
    {

    }
}

//Тупо заглушка, неиспользуемая система не хочу нечего ломать, пусть другие разбираются.