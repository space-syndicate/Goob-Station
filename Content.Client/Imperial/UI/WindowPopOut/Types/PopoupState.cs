using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Imperial.UI.WindowPopOut;


public sealed class PopoutState
{
    public readonly BaseWindow Window;
    public readonly Func<string?>? GetTitle;
    public TextureButton? Button;
    public IClydeWindow? PopOutWindow;
    public WindowRoot? PopOutRoot;
    public Vector2 PopInPosition;
    public bool CloseInProgress;
    public Action<WindowRequestClosedEventArgs>? PopOutClosedHandler;

    public PopoutState(BaseWindow window, Func<string?>? getTitle)
    {
        Window = window;
        GetTitle = getTitle;
    }
}
