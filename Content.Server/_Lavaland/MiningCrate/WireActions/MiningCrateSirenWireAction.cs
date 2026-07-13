// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Wires;
using Content.Shared._Lavaland.MiningCrate;
using Content.Shared.Popups;
using Content.Shared.Wires;

namespace Content.Server._Lavaland.MiningCrate.WireActions;

public sealed partial class MiningCrateSirenWireAction : ComponentWireAction<MiningCrateSecurityComponent>
{
    public override Color Color { get; set; } = Color.Orange;
    public override string Name { get; set; } = "wire-name-mining-crate-siren";
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = MiningCrateSecurityWireStatus.SirenIndicator;

    public override StatusLightState? GetLightState(Wire wire, MiningCrateSecurityComponent comp)
    {
        return comp.SirenWireIntact ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        return EntityManager.System<MiningCrateSecuritySystem>().SetSirenWireIntact(wire.Owner, false, user);
    }

    public override bool Mend(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        return EntityManager.System<MiningCrateSecuritySystem>().SetSirenWireIntact(wire.Owner, true, user);
    }

    public override void Pulse(EntityUid user, Wire wire, MiningCrateSecurityComponent comp)
    {
        EntityManager.System<SharedPopupSystem>().PopupEntity(
            Loc.GetString(comp.SirenWireIntact
                ? "lavaland-mining-crate-security-pulse-siren"
                : "lavaland-mining-crate-security-siren-cut"),
            wire.Owner,
            user);
    }
}
