using Content.Shared.Atmos;
using Content.Shared.Imperial.Atmos.Components;
using Content.Shared.Imperial.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Content.Client.Atmos.UI;

namespace Content.Client.Imperial.Atmos.UI;

/// <summary>
/// Initializes a <see cref="HydrogenPressurePumpWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class HydrogenPressurePumpBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private HydrogenPressurePumpWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HydrogenPressurePumpWindow>();

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.PumpOutputPressureChanged += OnPumpOutputPressurePressed;
        Update();
    }

    public override void Update()
    {
        if (_window == null)
            return;

        _window.Title = Identity.Name(Owner, EntMan);

        if (!EntMan.TryGetComponent(Owner, out HydrogenGasPressurePumpComponent? pump))
            return;

        _window.SetPumpStatus(pump.Enabled);
        _window.MaxPressure = pump.MaxTargetPressure;
        _window.SetOutputPressure(pump.TargetPressure);
    }

    private void OnToggleStatusButtonPressed(bool status)
    {
        SendPredictedMessage(new HydrogenGasPressurePumpToggleStatusMessage(status));
    }

    private void OnPumpOutputPressurePressed(float value)
    {
        SendPredictedMessage(new HydrogenGasPressurePumpChangeOutputPressureMessage(value));
    }
}
