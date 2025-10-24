using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Mobs.Phantomor;

public sealed partial class PhantomorSummonTentacleAction : InstantActionEvent
{
    /// <summary>
    /// кд между телепортациями моба
    /// </summary>
    [DataField("teleportCooldown")]
    public TimeSpan TeleportCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// звуковое сопровождение после телепортации
    /// </summary>
    [DataField("teleportSound")]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    /// <summary>
    /// длительность блокировки движения после телепортации
    /// </summary>
    [DataField("freezeWalking")]
    public TimeSpan FreezeWalking = TimeSpan.FromSeconds(3);

    /// <summary>
    /// длительность блокировки атаки после телепортации
    /// </summary>
    [DataField("freezeAttack")]
    public TimeSpan FreezeAttack = TimeSpan.FromSeconds(3);

    public readonly Dictionary<EntityUid, TimeSpan> LastTeleport = new();
}
