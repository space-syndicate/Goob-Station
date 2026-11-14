using System;
using System.Threading;
using Content.Client.Lobby;
using Content.Shared._BlackM.CCVar;
using Content.Shared._BlackM.DiscordAuth;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client._BlackM.DiscordAuth;

public sealed partial class DiscordAuthManager : Content.Corvax.Interfaces.Client.IClientDiscordAuthManager
{
    [Dependency] private IClientNetManager _netManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public string LinkCode { get; private set; } = string.Empty;
    public bool IsVerified { get; private set; } = false;
    public bool IsOpt { get; private set; }
    public bool IsEnabled { get; private set; }

    public event Action? DataUpdated;

    private bool _bypassed;

    private DiscordAuthWindow? _window;
    private CancellationTokenSource? _checkCancel;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgDiscordAuthByPass>();
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);
        _netManager.RegisterNetMessage<MsgDiscordAuthVerified>(OnDiscordAuthVerified);

        _cfg.OnValueChanged(BlackMCVars.DiscordAuthIsOptional, v => IsOpt = v, true);
        _cfg.OnValueChanged(BlackMCVars.DiscordAuthEnabled, v => IsEnabled = v, true);

        _netManager.Disconnect += OnDisconnected;
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs args)
    {
        _bypassed = false;
        IsVerified = false;
        LinkCode = string.Empty;
        CloseWindow();
    }

    private void OnDiscordAuthRequired(MsgDiscordAuthRequired message)
    {
        if (_bypassed)
            return;

        IsVerified = false;
        LinkCode = message.LinkCode;
        DataUpdated?.Invoke();

        if (message.ShowWindow)
            OpenWindow();
    }

    private void OnDiscordAuthVerified(MsgDiscordAuthVerified message)
    {
        IsVerified = true;
        DataUpdated?.Invoke();
        CloseWindow();

        if (_stateManager.CurrentState is DiscordAuthState)
            _stateManager.RequestStateChange<LobbyState>();
    }

    public void OpenWindow()
    {
        if (IsVerified)
            return;

        if (_window == null)
        {
            _window = new DiscordAuthWindow();
            _window.OnClose += OnWindowClosed;
        }

        _window.OpenCentered();
        _window.MoveToFront();

        if (_checkCancel == null)
        {
            _checkCancel = new CancellationTokenSource();
            SendCheck();
            Timer.SpawnRepeating(TimeSpan.FromSeconds(5), SendCheck, _checkCancel.Token);
        }
    }

    private void OnWindowClosed()
    {
        _checkCancel?.Cancel();
        _checkCancel = null;
    }

    private void CloseWindow()
    {
        _checkCancel?.Cancel();
        _checkCancel = null;

        if (_window == null)
            return;

        _window.OnClose -= OnWindowClosed;
        _window.Close();
        _window.Dispose();
        _window = null;
    }

    private void SendCheck()
    {
        if (_netManager.IsConnected)
            _netManager.ClientSendMessage(new MsgDiscordAuthCheck());
    }

    public void ByPass()
    {
        IsVerified = true;
        _bypassed = true;
        _netManager.ClientSendMessage(new MsgDiscordAuthByPass());
        CloseWindow();
    }
}
