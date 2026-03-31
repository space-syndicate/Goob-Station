using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Atmos.Piping.Trinary.Components
{
    [Serializable, NetSerializable]
    public enum HydrogenGasMixerUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasMixerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string MixerLabel { get; }
        public float OutputPressure { get; }
        public bool Enabled { get; }

        public float NodeOne { get; }

        public HydrogenGasMixerBoundUserInterfaceState(string mixerLabel, float outputPressure, bool enabled, float nodeOne)
        {
            MixerLabel = mixerLabel;
            OutputPressure = outputPressure;
            Enabled = enabled;
            NodeOne = nodeOne;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasMixerToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public HydrogenGasMixerToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasMixerChangeOutputPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; }

        public HydrogenGasMixerChangeOutputPressureMessage(float pressure)
        {
            Pressure = pressure;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasMixerChangeNodePercentageMessage : BoundUserInterfaceMessage
    {
        public float NodeOne { get; }

        public HydrogenGasMixerChangeNodePercentageMessage(float nodeOne)
        {
            NodeOne = nodeOne;
        }
    }
}
