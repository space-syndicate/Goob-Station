// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._CorvaxGoob.PowerToggle;
using Content.Shared._Lavaland.MiningCrate;

namespace Content.Server._Lavaland.MiningCrate;

public sealed class LavalandMiningCratePowerSystem : EntitySystem
{
    [Dependency] private readonly TogglePowerSystem _togglePower = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TogglePowerComponent, MiningCrateForcePowerOffEvent>(OnForcePowerOff);
    }

    private void OnForcePowerOff(Entity<TogglePowerComponent> ent, ref MiningCrateForcePowerOffEvent args)
    {
        if (args.Handled)
            return;

        _togglePower.SetPower(ent, powered: false, playSound: true);
        args.Handled = true;
    }
}
