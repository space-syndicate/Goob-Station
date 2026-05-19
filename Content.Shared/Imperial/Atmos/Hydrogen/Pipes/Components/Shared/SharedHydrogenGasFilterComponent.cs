using Robust.Shared.Serialization;
using Content.Shared.Atmos;

namespace Content.Shared.Imperial.Atmos.Piping.Trinary.Components
{
    [Serializable, NetSerializable]
    public enum HydrogenGasFilterUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasFilterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string FilterLabel { get; }
        public float TransferRate { get; }
        public bool Enabled { get; }
        public Gas? FilteredGas { get; }

        public HydrogenGasFilterBoundUserInterfaceState(string filterLabel, float transferRate, bool enabled, Gas? filteredGas)
        {
            FilterLabel = filterLabel;
            TransferRate = transferRate;
            Enabled = enabled;
            FilteredGas = filteredGas;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasFilterToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public HydrogenGasFilterToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasFilterChangeRateMessage : BoundUserInterfaceMessage
    {
        public float Rate { get; }

        public HydrogenGasFilterChangeRateMessage(float rate)
        {
            Rate = rate;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasFilterSelectGasMessage(Gas? gas) : BoundUserInterfaceMessage
    {
        public readonly Gas? Gas = gas;
    }
}
