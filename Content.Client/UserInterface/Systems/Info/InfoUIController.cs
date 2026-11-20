// SPDX-FileCopyrightText: 2022 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr.@gmail.com>
// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2022 Jezithyr <jmaster9999@gmail.com>
// SPDX-FileCopyrightText: 2022 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 wrexbe <wrexbe@protonmail.com>
// SPDX-FileCopyrightText: 2024 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Audio;
using Content.Client._BlackM.Rules;
using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Shared.Guidebook;
using Content.Shared.Info;
using Robust.Client;
using Robust.Client.Audio;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class InfoUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!; // #BlackM
    [Dependency] private readonly IResourceCache _resourceCache = default!; // #BlackM
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!; // #BlackM

    private RulesArrivalPopup? _rulesPopup; // BlackM: arrival and paper folder.
    private RulesAndInfoWindow? _infoWindow;
    private IAudioSource? _rulesMusic; // #BlackM
    private float? _pendingRulesTime;

    private static readonly ProtoId<GuideEntryPrototype> DefaultRuleset = "DefaultRuleset";

    public ProtoId<GuideEntryPrototype> RulesEntryId = DefaultRuleset;

    protected override string SawmillName => "rules";

    public override void Initialize()
    {
        base.Initialize();

        _netManager.RegisterNetMessage<RulesAcceptedMessage>();
        _netManager.RegisterNetMessage<SendRulesInformationMessage>(OnRulesInformationMessage);
        _netManager.Disconnect += OnRulesDisconnected;
        _baseClient.PlayerJoinedServer += OnPlayerJoinedServer;

        _consoleHost.RegisterCommand("fuckrules",
            "",
            "",
            (_, _, _) =>
        {
            OnAcceptPressed();
        });
    }

    private void OnRulesInformationMessage(SendRulesInformationMessage message)
    {
        RulesEntryId = message.CoreRules;

        if (message.ShouldShowRules)
            ShowRules(message.PopupTime);
    }

    private void OnPlayerJoinedServer(object? sender, PlayerEventArgs args)
    {
        if (_pendingRulesTime is not { } time)
            return;

        _pendingRulesTime = null;
        ShowRules(time);
    }

    private void OnRulesDisconnected(object? sender, NetDisconnectedArgs args)
    {
        _pendingRulesTime = null;
        var popup = _rulesPopup;
        _rulesPopup = null;
        popup?.Dispose();
        StopRulesMusic();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_infoWindow == null)
            return;

        _infoWindow.Dispose();
        _infoWindow = null;
    }

    private void ShowRules(float time)
    {
        // Network rules can arrive before GameStartedSetup initializes entity systems.
        if (_baseClient.RunLevel < ClientRunLevel.Connected)
        {
            _pendingRulesTime = time;
            return;
        }

        if (_rulesPopup != null)
            return;

        _entitySystemManager.GetEntitySystem<ContentAudioSystem>().PauseForRules();
        _rulesPopup = new RulesArrivalPopup();
        _rulesPopup.Initialize(time, false);

        _rulesPopup.OnQuitPressed += OnQuitPressed;
        _rulesPopup.OnAcceptPressed += OnAcceptPressed;
        UIManager.WindowRoot.AddChild(_rulesPopup);
        LayoutContainer.SetAnchorPreset(_rulesPopup, LayoutContainer.LayoutPreset.Wide);
        StartRulesMusic(); // #BlackM
    }

    // #BlackM
    private void StartRulesMusic()
    {
        var resource = _resourceCache.GetResource<AudioResource>("/Audio/_BlackM/rules_theme.ogg");
        var stream = _audioManager.CreateAudioSource(resource);
        if (stream == null) return;
        stream.Looping = true;
        stream.Gain = 0.3f;
        stream.StartPlaying();
        _rulesMusic = stream;
    }

    // #BlackM
    private void StopRulesMusic()
    {
        if (_rulesMusic == null) return;
        _rulesMusic.StopPlaying();
        _rulesMusic.Dispose();
        _rulesMusic = null;
        _entitySystemManager.GetEntitySystem<ContentAudioSystem>().ResumeAfterRules();
    }

    private void OnQuitPressed()
    {
        StopRulesMusic(); // #BlackM
        _consoleHost.ExecuteCommand("quit");
    }

    private void OnAcceptPressed()
    {
        var popup = _rulesPopup;
        _rulesPopup = null;
        popup?.Dispose();
        StopRulesMusic(); // #BlackM
        _netManager.ClientSendMessage(new RulesAcceptedMessage());
    }

    public GuideEntryPrototype GetCoreRuleEntry()
    {
        if (!_prototype.TryIndex(RulesEntryId, out var guideEntryPrototype))
        {
            guideEntryPrototype = _prototype.Index(DefaultRuleset);
            Log.Error($"Couldn't find the following prototype: {RulesEntryId}. Falling back to {DefaultRuleset}, please check that the server has the rules set up correctly");
            return guideEntryPrototype;
        }

        return guideEntryPrototype;
    }

    public void OpenWindow()
    {
        if (_infoWindow == null || _infoWindow.Disposed)
            _infoWindow = UIManager.CreateWindow<RulesAndInfoWindow>();

        _infoWindow?.OpenCentered();
    }
}
