using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Atmos.Piping.Binary.Components
{
    public sealed record HydrogenGasVolumePumpData(float LastMolesTransferred);

    [Serializable, NetSerializable]
    public enum HydrogenGasVolumePumpUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasVolumePumpToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public HydrogenGasVolumePumpToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HydrogenGasVolumePumpChangeTransferRateMessage : BoundUserInterfaceMessage
    {
        public float TransferRate { get; }

        public HydrogenGasVolumePumpChangeTransferRateMessage(float transferRate)
        {
            TransferRate = transferRate;
        }
    }
}
