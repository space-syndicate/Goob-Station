using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Power.Generation.PPG
{

    [Serializable, NetSerializable]
    public enum PPGVisualsState
    {
        PowerOutput,
    }

    [Serializable, NetSerializable]
    public enum PPGVisualLayers
    {
        PowerOutput,
        Off
    }
}

