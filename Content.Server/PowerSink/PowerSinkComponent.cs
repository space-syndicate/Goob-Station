using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.PowerSink
{
    /// <summary>
    /// Absorbs power up to its capacity when anchored then explodes.
    /// </summary>
    [RegisterComponent, AutoGenerateComponentPause]
    public sealed partial class PowerSinkComponent : Component
    {
        /// <summary>
        /// When the power sink is nearing its explosion, warn the crew so they can look for it
        /// (if they're not already).
        /// </summary>
        [DataField("sentImminentExplosionWarning")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool SentImminentExplosionWarningMessage = false;

        /// <summary>
        /// If explosion has been triggered, time at which to explode.
        /// </summary>
        [DataField("explosionTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
        [AutoPausedField]
        public System.TimeSpan? ExplosionTime = null;

        /// <summary>
        /// The highest sound warning threshold that has been hit (plays sfx occasionally as explosion nears)
        /// </summary>
        [DataField("highestWarningSoundThreshold")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float HighestWarningSoundThreshold = 0f;

        [DataField("chargeFireSound")]
        public SoundSpecifier ChargeFireSound = new SoundPathSpecifier("/Audio/Effects/PowerSink/charge_fire.ogg");

        [DataField("electricSound")] public SoundSpecifier ElectricSound =
            new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg")
            {
                Params = AudioParams.Default
                    .WithVolume(15f) // audible even behind walls
                    .WithRolloffFactor(10)
            };

        // Imperial Space Pirates: New Horizon; Start

        /// <summary>
        /// If PowerSink reached it's full charge, defines if it explodes.
        /// </summary>
        [DataField("explodeOnFullCharge")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ExplodeOnFullCharge = true;
        /// <summary>
        /// The message, sent when power sink is nearing it's full charge
        /// </summary>
        [DataField("imminentExplosionMessage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string ImminentExplosionMessage = "powersink-immiment-explosion-announcement";

        // Imperial Space Pirates: New Horizon; End
    }
}
