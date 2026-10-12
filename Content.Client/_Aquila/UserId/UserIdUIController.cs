using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Aquila.UserId;

/// <summary>
/// Открывает окно с UUID игрока из лобби и меню ESC.
/// </summary>
public sealed class UserIdUIController : UIController
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private UserIdWindow? _window;

    public void ToggleWindow()
    {
        if (_window is { IsOpen: true })
        {
            _window.Close();
            return;
        }

        if (_window == null)
        {
            _window = UIManager.CreateWindow<UserIdWindow>();
            _window.OnClose += () => _window = null;
        }

        _window.SetUserId(_player.LocalUser?.UserId.ToString() ?? string.Empty);
        _window.OpenCentered();
    }
}
