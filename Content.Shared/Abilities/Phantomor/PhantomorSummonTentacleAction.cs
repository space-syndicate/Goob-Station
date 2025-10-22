using System.ComponentModel.DataAnnotations;
using Content.Shared.Actions;

namespace Content.Shared.Teleportation;

public sealed partial class PhantomorSummonTentacleAction : InstantActionEvent
{
    /// <summary>
    /// кд между телепортациями моба
    /// </summary>
    [DataField("TeleportCooldown")]
    public float teleportCooldown = 30f;

    /// <summary>
    /// звуковое сопровождение после телепортации
    /// </summary>
    [DataField("TeleportSound")]
    public string teleportSound = "/Audio/Items/bikehorn.ogg";

    /// <summary>
    /// длительность блокировки движения после телепортации
    /// </summary>
    [DataField("FreezeWalking")]
    public float freezeWalking = 0.75f;

    /// <summary>
    /// длительность блокировки атаки после телепортации
    /// </summary>
    [DataField("FreezeAttack")]
    public float freezeAttack = 0.75f;
}
