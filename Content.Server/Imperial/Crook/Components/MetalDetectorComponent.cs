using Robust.Shared.Audio;
using Content.Shared.Imperial.Crook.Visuals;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;

namespace Content.Server.Imperial.Crook.Components
{
    [RegisterComponent]
    public sealed partial class MetalDetectorComponent : Component
    {
        [DataField("detectionRange")]
        public float DetectionRange = 1f;

        [ViewVariables]
        public HashSet<EntityUid> CollidingEntities = new();

        [DataField("checkedSlots")]
        public HashSet<string> CheckedSlots = new()
        {
            "pocket1",
            "pocket2",
            "belt",
            "backpack",
            "idCard",
            "suitStorage",
            "outerClothing"
        };

        [DataField("stateResetDelay")]
        public TimeSpan StateResetDelay = TimeSpan.FromSeconds(2);

        [DataField("maxRecursionDepth")]
        public int MaxRecursionDepth = 5;

        [DataField("allowedAccess")]
        public List<string> AllowedAccess = new() { "Security", "Command" };

        [DataField("checkContraband")]
        public bool CheckContraband = true;

        [DataField("shockDuration")]
        public TimeSpan ShockDuration = TimeSpan.FromSeconds(1);

        [DataField("shockDamage")]
        public DamageSpecifier ShockDamage = new()
        {
            DamageDict = new()
            {
                { "Shock", 5f }
            }
        };

        [DataField("shockSound")]
        public SoundSpecifier ShockSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        [ViewVariables]
        [DataField("nextStateReset", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextStateReset;

        [DataField("clearSound")]
        public SoundSpecifier ClearSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

        [DataField("alertSound")]
        public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

        [ViewVariables]
        public bool Powered;

        [ViewVariables]
        public bool Emagged;

        [ViewVariables]
        public MetalDetectorVisualState State = MetalDetectorVisualState.Off;
    }
}
