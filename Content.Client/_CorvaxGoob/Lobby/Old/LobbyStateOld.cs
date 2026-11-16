using Content.Client._CorvaxGoob.Lobby.Old;
using Content.Client.Audio;
using Content.Client.GameTicking.Managers;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.CCVar;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Lobby
{
    public sealed partial class LobbyState
    {
        public OldLobbyGui? OldLobby;

        private void StartupOld()
        {
            if (_userInterfaceManager.ActiveScreen == null)
            {
                return;
            }

            OldLobby = (OldLobbyGui) _userInterfaceManager.ActiveScreen;

            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            _gameTicker = _entityManager.System<ClientGameTicker>();
            _contentAudioSystem = _entityManager.System<ContentAudioSystem>();
            _contentAudioSystem.LobbySoundtrackChanged += UpdateOldLobbySoundtrackInfo;
            _sawmill = Logger.GetSawmill("lobby");

            chatController.SetMainChat(true);

            _voteManager.SetPopupContainer(OldLobby.VoteContainer);
            LayoutContainer.SetAnchorPreset(OldLobby, LayoutContainer.LayoutPreset.Wide);

            var lobbyNameCvar = _cfg.GetCVar(CCVars.ServerLobbyName);
            var serverName = _baseClient.GameInfo?.ServerName ?? string.Empty;

            OldLobby.ServerName.Text = string.IsNullOrEmpty(lobbyNameCvar)
                ? Loc.GetString("ui-lobby-title", ("serverName", serverName))
                : lobbyNameCvar;

            var width = _cfg.GetCVar(CCVars.ServerLobbyRightPanelWidth);
            OldLobby.RightSide.SetWidth = width;

            UpdateOldLobbyUi();

            OldLobby.CharacterPreview.CharacterSetupButton.OnPressed += OnOldSetupPressed;
            OldLobby.ReadyButton.OnPressed += OnReadyPressed;
            OldLobby.ReadyButton.OnToggled += OnReadyToggled;

            _gameTicker.InfoBlobUpdated += UpdateOldLobbyUi;
            _gameTicker.LobbyStatusUpdated += OldLobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated += OldLobbyLateJoinStatusUpdated;
        }

        private void ShutdownOld()
        {
            var chatController = _userInterfaceManager.GetUIController<ChatUIController>();
            chatController.SetMainChat(false);
            OldLobby!.Chat.Main = false;
            _gameTicker.InfoBlobUpdated -= UpdateOldLobbyUi;
            _gameTicker.LobbyStatusUpdated -= OldLobbyStatusUpdated;
            _gameTicker.LobbyLateJoinStatusUpdated -= OldLobbyLateJoinStatusUpdated;
            _contentAudioSystem.LobbySoundtrackChanged -= UpdateOldLobbySoundtrackInfo;

            _voteManager.ClearPopupContainer();

            OldLobby!.CharacterPreview.CharacterSetupButton.OnPressed -= OnOldSetupPressed;
            OldLobby!.ReadyButton.OnPressed -= OnReadyPressed;
            OldLobby!.ReadyButton.OnToggled -= OnReadyToggled;

            OldLobby = null;
        }

        public void SwitchState(OldLobbyGui.LobbyGuiState state)
        {
            // Yeah I hate this but LobbyState contains all the badness for now.
            OldLobby?.SwitchState(state);
        }

        private void OnOldSetupPressed(BaseButton.ButtonEventArgs args)
        {
            SetReady(false);
            OldLobby?.SwitchState(OldLobbyGui.LobbyGuiState.CharacterSetup);
        }

        private void FrameUpdateOld(FrameEventArgs e)
        {
            if (_gameTicker.IsGameStarted)
            {
                OldLobby!.StartTime.Text = string.Empty;
                var roundTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                OldLobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-time", ("hours", roundTime.Hours), ("minutes", roundTime.Minutes));
                return;
            }

            OldLobby!.StationTime.Text = Loc.GetString("lobby-state-player-status-round-not-started");
            string text;

            if (_gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else if (_gameTicker.StartTime < _gameTiming.CurTime)
            {
                OldLobby!.StartTime.Text = Loc.GetString("lobby-state-soon");
                return;
            }
            else
            {
                var difference = _gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5 ? "lobby-state-right-now-question" : "lobby-state-right-now-confirmation");
                }
                else if (difference.TotalHours >= 1)
                {
                    text = $"{Math.Floor(difference.TotalHours)}:{difference.Minutes:D2}:{difference.Seconds:D2}";
                }
                else
                {
                    text = $"{difference.Minutes}:{difference.Seconds:D2}";
                }
            }

            OldLobby!.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void OldLobbyStatusUpdated()
        {
            UpdateOldLobbyBackground();
            UpdateOldLobbyUi();
        }

        private void OldLobbyLateJoinStatusUpdated()
        {
            OldLobby!.ReadyButton.Disabled = _gameTicker.DisallowedLateJoin;
        }

        private void UpdateOldLobbyUi()
        {
            if (_gameTicker.IsGameStarted)
            {
                OldLobby!.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-join-state");
                OldLobby!.ReadyButton.ToggleMode = false;
                OldLobby!.ReadyButton.Pressed = false;
                OldLobby!.ObserveButton.Disabled = false;
            }
            else
            {
                OldLobby!.StartTime.Text = string.Empty;
                OldLobby!.ReadyButton.Pressed = _gameTicker.AreWeReady;
                OldLobby!.ReadyButton.Text = Loc.GetString(OldLobby!.ReadyButton.Pressed ? "lobby-state-player-status-ready": "lobby-state-player-status-not-ready");
                OldLobby!.ReadyButton.ToggleMode = true;
                OldLobby!.ReadyButton.Disabled = false;
                OldLobby!.ObserveButton.Disabled = true;
            }

            if (_gameTicker.ServerInfoBlob != null)
            {
                OldLobby!.ServerInfo.SetInfoBlob(_gameTicker.ServerInfoBlob);
            }

            var minutesToday = _playtimeTracking.PlaytimeMinutesToday;
            if (minutesToday > 60)
            {
                OldLobby!.PlaytimeComment.Visible = true;

                var hoursToday = Math.Round(minutesToday / 60f, 1);

                var chosenString = minutesToday switch
                {
                    < 180 => "lobby-state-playtime-comment-normal",
                    < 360 => "lobby-state-playtime-comment-concerning",
                    < 720 => "lobby-state-playtime-comment-grasstouchless",
                    _ => "lobby-state-playtime-comment-selfdestructive"
                };

                OldLobby.PlaytimeComment.SetMarkup(Loc.GetString(chosenString, ("hours", hoursToday)));
            }
            else
                OldLobby!.PlaytimeComment.Visible = false;
        }

        private void UpdateOldLobbySoundtrackInfo(LobbySoundtrackChangedEvent ev)
        {
            if (ev.SoundtrackFilename == null)
            {
                OldLobby!.LobbySong.SetMarkup(Loc.GetString("lobby-state-song-no-song-text"));
            }
            else if (
                ev.SoundtrackFilename != null
                && _resourceCache.TryGetResource<AudioResource>(ev.SoundtrackFilename, out var lobbySongResource)
                )
            {
                var lobbyStream = lobbySongResource.AudioStream;

                var title = string.IsNullOrEmpty(lobbyStream.Title)
                    ? Loc.GetString("lobby-state-song-unknown-title")
                    : lobbyStream.Title;

                var artist = string.IsNullOrEmpty(lobbyStream.Artist)
                    ? Loc.GetString("lobby-state-song-unknown-artist")
                    : lobbyStream.Artist;

                var markup = Loc.GetString("lobby-state-song-text",
                    ("songTitle", title),
                    ("songArtist", artist));

                OldLobby!.LobbySong.SetMarkup(markup);
            }
        }

        // Goobstation - heavily modified to add credits for lobby backgrounds
        private void UpdateOldLobbyBackground()
        {
            if (_gameTicker.LobbyBackground != null)
            {
                var lobbyBackground = _protoMan.Index(_gameTicker.LobbyBackground.Value);
                OldLobby!.Background.Texture = _resourceCache.GetResource<TextureResource>(lobbyBackground.Background);

                var name = string.IsNullOrEmpty(lobbyBackground.Name)
                    ? Loc.GetString("lobby-state-background-unknown-title")
                    : lobbyBackground.Name;

                var artist = string.IsNullOrEmpty(lobbyBackground.Artist)
                    ? Loc.GetString("lobby-state-background-unknown-artist")
                    : lobbyBackground.Artist;

                var markup = Loc.GetString("lobby-state-background-text",
                    ("backgroundName", name),
                    ("backgroundArtist", artist));

                OldLobby!.LobbyBackground.SetMarkup(markup);

                return;
            }

            _sawmill.Warning("_gameTicker.LobbyBackground was null! No lobby background selected.");
            OldLobby!.Background.Texture = null;
            OldLobby!.LobbyBackground.SetMarkup(Loc.GetString("lobby-state-background-no-background-text"));
        }
    }
}
