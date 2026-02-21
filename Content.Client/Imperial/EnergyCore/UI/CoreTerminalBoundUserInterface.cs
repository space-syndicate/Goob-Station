// Компоненты консоли ядра
using JetBrains.Annotations;
using Content.Shared.Imperial.EnergyCore;
using Robust.Client.UserInterface;

namespace Content.Client.Imperial.EnergyCore.UI
{
    [UsedImplicitly]
    public sealed class CoreTerminalBoundUserInterface : BoundUserInterface
    {
        private CoreTerminalWindow? _window;

        public CoreTerminalBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<CoreTerminalWindow>();
            _window.OnCoreTerminalButton += ButtonPressed;
        }
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (CoreTerminalBoundUserInterfaceState)state;
            _window?.UpdateState(castState); //Update window state
        }

        public void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }
    }
}
