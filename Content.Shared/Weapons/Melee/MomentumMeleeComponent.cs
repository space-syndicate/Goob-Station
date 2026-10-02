using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// While this item is held in a hand, moving into a mob (in the direction of movement)
/// deals damage scaled by the holder's movement speed, Minecraft-spear style.
/// The held item has no active physics body, so the holder's velocity is used and
/// targets are found with a proactive lookup in a forward cone each tick.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MomentumMeleeComponent : Component
{
    /// <summary>
    /// Base damage payload, scaled by the speed factor. Applied per hit.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Holder must move at least this fast (m/s) for the effect to trigger.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinimumSpeed = 4.5f;

    /// <summary>
    /// Multiplier in the damage formula: Damage * SpeedDamageFactor * speed / MinimumSpeed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpeedDamageFactor = 0.5f;

    /// <summary>
    /// Contact range (metres) from the holder within which mobs are hit.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 0.6f;

    /// <summary>
    /// Full width (degrees) of the forward cone in the direction of movement.
    /// Targets outside this cone are ignored.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ArcDegrees = 90f;

    /// <summary>
    /// Per-target cooldown (seconds) between hits.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageCooldown = 1f;

    /// <summary>
    /// Sound played on a successful hit.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SoundHit = new SoundCollectionSpecifier("MetalThud");

    /// <summary>
    /// If true and the item has an <see cref="Content.Shared.Projectiles.EmbeddableProjectileComponent"/>,
    /// a successful hit drops the item from the hand and embeds it into the target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool EmbedOnHit = false;

    /// <summary>
    /// Per-target time of last hit, used for the cooldown. Not serialized.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> LastHit = new();
}
