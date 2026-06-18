using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> меняет проджектайл у бейсик проджектайл провайдера.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeProjectileComponent : Component
{
    /// <summary>
    /// На какой прототип меняеееем
    /// </summary>
    [DataField]
    public EntProtoId Proto;
}
