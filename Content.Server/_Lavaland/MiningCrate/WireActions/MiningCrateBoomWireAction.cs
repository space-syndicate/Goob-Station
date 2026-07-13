// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Wires;
using Content.Shared._Lavaland.MiningCrate;
using Content.Shared.Wires;

namespace Content.Server._Lavaland.MiningCrate.WireActions;

public sealed partial class MiningCrateBoomWireAction : ComponentWireAction<MiningCrateSecurityComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-mining-crate-boom";
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = MiningCrateSecurityWireStatus.BoomIndicator;

    public override StatusLightState? GetLightState(Wire wire, MiningCrateSecurityComponent comp) => null;

    public override bool AddWire(Wire wire, int count) => true;

    public override bool Cut(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        return EntityManager.System<MiningCrateSecuritySystem>().OnBoomWireCut(wire.Owner, user);
    }

    public override bool Mend(EntityUid user, Wire wire, MiningCrateSecurityComponent comp) => true;

    public override void Pulse(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        EntityManager.System<MiningCrateSecuritySystem>().OnBoomWirePulse(wire.Owner, user);
    }
}
