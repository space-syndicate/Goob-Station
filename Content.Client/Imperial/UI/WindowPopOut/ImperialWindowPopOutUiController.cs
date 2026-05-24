using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Imperial.UI.WindowPopOut;

/// <summary>
/// Adds pop-out buttons to <see cref="DefaultWindow"/> instances when they are opened.
/// </summary>
public sealed class ImperialWindowPopOutUiController : UIController
{
    public override void Initialize()
    {
        UIManager.WindowRoot.OnChildAdded += OnRootChildAdded;
    }

    private void OnRootChildAdded(Control child)
    {
        if (child is DefaultWindow defaultWindow)
            ImperialWindowPopOut.Enable(defaultWindow, () => defaultWindow.Title);
    }
}
