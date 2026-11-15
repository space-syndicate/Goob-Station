using Content.Client._CorvaxGoob.Lobby.Old;
using Content.Client.Lobby.UI;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared._CorvaxGoob.CCCVars;
using Robust.Shared.Timing;

namespace Content.Client.Lobby
{
    public sealed partial class LobbyState
    {
        private bool? _oldLobbyMode;

        public bool IsOldLobby
        {
            get
            {
                if (_oldLobbyMode == null)
                    _oldLobbyMode = _cfg.GetCVar(CCCVars.OldLobby);

                return _oldLobbyMode.Value;
            }
        }

        private Type LobbyScreenType => IsOldLobby ? typeof(OldLobbyGui) : typeof(LobbyGui);

        public event Action<LobbyState>? LobbySwitched;

        private bool _switchLobbyPending;

        public void RequestSwitchLobby()
        {
            _switchLobbyPending = true;
        }

        private void SwitchLobby()
        {
            var oldLobby = _cfg.GetCVar(CCCVars.OldLobby);

            if (oldLobby == IsOldLobby)
                return;

            var ahelpController = _userInterfaceManager.GetUIController<AHelpUIController>();
            var lobbyController = _userInterfaceManager.GetUIController<LobbyUIController>();
            var oldLobbyController = _userInterfaceManager.GetUIController<OldLobbyUIController>();

            ahelpController.OnStateExited(this);
            lobbyController.OnStateExited(this);
            oldLobbyController.OnStateExited(this);

            Shutdown();
            _userInterfaceManager.UnloadScreen();

            _oldLobbyMode = oldLobby;

            if (oldLobby)
                _userInterfaceManager.LoadScreen<OldLobbyGui>();
            else
                _userInterfaceManager.LoadScreen<LobbyGui>();

            Startup();

            if (oldLobby)
            {
                OldLobbyStatusUpdated();
                OldLobbyLateJoinStatusUpdated();
            }
            else
            {
                LobbyStatusUpdated();
                LobbyLateJoinStatusUpdated();
            }

            ahelpController.OnStateEntered(this);
            lobbyController.OnStateEntered(this);
            oldLobbyController.OnStateEntered(this);

            LobbySwitched?.Invoke(this);
        }

        private bool TryStartupOld()
        {
            if (!IsOldLobby)
                return false;

            StartupOld();
            return true;
        }

        private bool TryShutdownOld()
        {
            if (!IsOldLobby)
                return false;

            ShutdownOld();
            return true;
        }

        private bool TryFrameUpdateOld(FrameEventArgs e)
        {
            if (_switchLobbyPending)
            {
                _switchLobbyPending = false;
                SwitchLobby();
            }

            if (!IsOldLobby)
                return false;

            FrameUpdateOld(e);
            return true;
        }
    }
}
