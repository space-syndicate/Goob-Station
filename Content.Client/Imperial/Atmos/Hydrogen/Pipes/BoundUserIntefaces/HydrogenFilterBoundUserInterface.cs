using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Imperial.Atmos.Piping.Trinary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Content.Client.Atmos.UI;

namespace Content.Client.Imperial.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="HydrogenFilterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class HydrogenFilterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private HydrogenFilterWindow? _window;

        public HydrogenFilterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var atmosSystem = EntMan.System<AtmosphereSystem>();

            _window = this.CreateWindow<HydrogenFilterWindow>();
            _window.PopulateGasList(atmosSystem.Gases);

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.FilterTransferRateChanged += OnFilterTransferRatePressed;
            _window.SelectGasPressed += OnSelectGasPressed;
        }

        private void OnToggleStatusButtonPressed(bool status)
        {
            SendMessage(new HydrogenGasFilterToggleStatusMessage(status));
        }

        private void OnFilterTransferRatePressed(string value)
        {
            var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;

            SendMessage(new HydrogenGasFilterChangeRateMessage(rate));
        }

        private void OnSelectGasPressed()
        {
            if (_window is null)
                return;

            if (_window.SelectedGas is null)
            {
                SendMessage(new HydrogenGasFilterSelectGasMessage(null));
            }
            else
            {
                if (!Enum.TryParse<Gas>(_window.SelectedGas, out var gas))
                    return;

                SendMessage(new HydrogenGasFilterSelectGasMessage(gas));
            }
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not HydrogenGasFilterBoundUserInterfaceState cast)
                return;

            _window.Title = cast.FilterLabel;
            _window.SetFilterStatus(cast.Enabled);
            _window.SetTransferRate(cast.TransferRate);
            if (cast.FilteredGas is not null)
            {
                var atmos = EntMan.System<AtmosphereSystem>();
                var gas = atmos.GetGas((Gas)cast.FilteredGas);
                var gasName = Loc.GetString(gas.Name);
                _window.SetGasFiltered(gas.ID, gasName);
            }
            else
            {
                _window.SetGasFiltered(null, Loc.GetString("comp-gas-filter-ui-filter-gas-none"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
