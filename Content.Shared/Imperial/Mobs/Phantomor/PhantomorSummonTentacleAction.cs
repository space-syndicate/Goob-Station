using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Mobs.Phantomor;

public sealed partial class PhantomorSummonTentacleAction : InstantActionEvent
{
    /// <summary>
    /// кд между телепортациями моба
    /// </summary>
    [DataField]
    public TimeSpan TeleportCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// звуковое сопровождение после телепортации
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };

    /// <summary>
    /// длительность блокировки движения после телепортации
    /// </summary>
    [DataField]
    public TimeSpan FreezeWalking = TimeSpan.FromSeconds(3);

    /// <summary>
    /// длительность блокировки атаки после телепортации
    /// </summary>
    [DataField]
    public TimeSpan FreezeAttack = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan ShakingTime = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public readonly Dictionary<EntityUid, TimeSpan> LastTeleport = new();
}
