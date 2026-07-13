// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Wires;
using Content.Shared._Lavaland.MiningCrate;
using Content.Shared.Popups;
using Content.Shared.Wires;

namespace Content.Server._Lavaland.MiningCrate.WireActions;

public sealed partial class MiningCrateLockWireAction : ComponentWireAction<MiningCrateSecurityComponent>
{
    public override Color Color { get; set; } = Color.Gold;
    public override string Name { get; set; } = "wire-name-mining-crate-lock";
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = MiningCrateSecurityWireStatus.LockIndicator;

    public override StatusLightState? GetLightState(Wire wire, MiningCrateSecurityComponent comp)
    {
        return comp.LockWireCut ? StatusLightState.BlinkingFast : StatusLightState.On;
    }

    public override bool AddWire(Wire wire, int count) => true;

    public override bool Cut(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        return EntityManager.System<MiningCrateSecuritySystem>().SetLockWireCut(wire.Owner, true, user);
    }

    public override bool Mend(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        var anyOtherCut = false;
        foreach (var w in EntityManager.System<WiresSystem>().TryGetWires<MiningCrateLockWireAction>(wire.Owner))
        {
            if (w.Id != wire.Id && w.IsCut)
                anyOtherCut = true;
        }

        if (anyOtherCut)
            return true;

        return EntityManager.System<MiningCrateSecuritySystem>().SetLockWireCut(wire.Owner, false, user);
    }

    public override void Pulse(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        EntityManager.System<SharedPopupSystem>().PopupEntity(
            Loc.GetString("lavaland-mining-crate-security-pulse-lock"),
            wire.Owner,
            user);
    }
}
