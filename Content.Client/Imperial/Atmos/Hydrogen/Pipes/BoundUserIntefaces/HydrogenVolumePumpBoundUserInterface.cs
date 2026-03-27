using Content.Shared.Imperial.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Content.Client.Atmos.UI;

namespace Content.Client.Imperial.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="HydrogenVolumePumpWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class HydrogenVolumePumpBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private float _maxTransferRate;

        [ViewVariables]
        private HydrogenVolumePumpWindow? _window;

        public HydrogenVolumePumpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<HydrogenVolumePumpWindow>();

            if (EntMan.TryGetComponent(Owner, out HydrogenGasVolumePumpComponent? pump))
            {
                _maxTransferRate = pump.MaxTransferRate;
            }

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.PumpTransferRateChanged += OnPumpTransferRatePressed;
            Update();
        }

        private void OnToggleStatusButtonPressed(bool status)
        {
            SendPredictedMessage(new HydrogenGasVolumePumpToggleStatusMessage(status));
        }

        private void OnPumpTransferRatePressed(string value)
        {
            var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
            rate = Math.Clamp(rate, 0f, _maxTransferRate);

            SendPredictedMessage(new HydrogenGasVolumePumpChangeTransferRateMessage(rate));
        }

        public override void Update()
        {
            base.Update();

            if (_window is null || !EntMan.TryGetComponent(Owner, out HydrogenGasVolumePumpComponent? pump))
                return;

            _window.Title = Identity.Name(Owner, EntMan);
            _window.SetPumpStatus(pump.Enabled);
            _window.SetTransferRate(pump.TransferRate);
        }
    }
}
