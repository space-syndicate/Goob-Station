using Content.Client.Gameplay;
using Content.Client.Guidebook;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared._CorvaxGoob.CCCVars;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.Lobby.Old;

public sealed class OldCharacterWindowUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly JobRequirementsManager _requirements = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [UISystemDependency] private readonly GuidebookSystem? _guide = default!;

    private DefaultWindow? _characterWindow;
    private OldCharacterSetupGui? _characterSetup;
    private OldHumanoidProfileEditor? _profileEditor;

    public void OnStateExited(GameplayState state)
    {
        CloseCharacterSetup();
    }

    public bool TryOpenCharacterSetup()
    {
        if (!_cfg.GetCVar(CCCVars.OldLobby))
            return false;

        OpenCharacterSetup();
        return true;
    }

    private void OpenCharacterSetup()
    {
        if (_characterWindow is { IsOpen: true })
        {
            _characterWindow.MoveToFront();
            return;
        }

        _profileEditor = new OldHumanoidProfileEditor(
            _preferencesManager,
            _cfg,
            EntityManager,
            _dialogManager,
            LogManager,
            _playerManager,
            _prototypeManager,
            _resourceCache,
            _requirements,
            _markings);

        if (_guide != null)
            _profileEditor.OnOpenGuidebook += _guide.OpenHelp;

        _profileEditor.Save += SaveCharacterProfile;

        _characterSetup = new OldCharacterSetupGui(_profileEditor);

        _characterSetup.CloseButton.OnPressed += _ =>
        {
            if (_profileEditor.Profile != null && _profileEditor.IsDirty)
            {
                OpenCharacterSavePanel();
                return;
            }

            CloseCharacterSetup();
        };

        _characterSetup.SelectCharacter += slot =>
        {
            _preferencesManager.SelectCharacter(slot);
            ReloadCharacterSetup();
        };

        _characterSetup.DeleteCharacter += slot =>
        {
            _preferencesManager.DeleteCharacter(slot);

            if (_profileEditor.CharacterSlot == slot)
                ReloadCharacterSetup();
            else
                _characterSetup.ReloadCharacterPickers();
        };

        _characterWindow = new DefaultWindow
        {
            Title = Loc.GetString("ui-escape-character"),
            Resizable = true,
            MinWidth = 1050,
            MinHeight = 550,
            SetWidth = 1050,
            SetHeight = 700
        };

        _characterWindow.Contents.AddChild(_characterSetup);

        if (_preferencesManager.ServerDataLoaded)
        {
            _characterSetup.ReloadCharacterPickers();
            _profileEditor.SetProfile(
                (HumanoidCharacterProfile?) _preferencesManager.Preferences?.SelectedCharacter,
                _preferencesManager.Preferences?.SelectedCharacterIndex);
        }

        _characterWindow.OpenCentered();
    }

    private void CloseCharacterSetup()
    {
        _characterWindow?.Dispose();
        _characterWindow = null;
        _characterSetup = null;
        _profileEditor = null;
    }

    private void SaveCharacterProfile()
    {
        if (_profileEditor?.Profile == null || _profileEditor.CharacterSlot == null)
            return;

        _preferencesManager.UpdateCharacter(_profileEditor.Profile, _profileEditor.CharacterSlot.Value);
        ReloadCharacterSetup();
    }

    private void ReloadCharacterSetup()
    {
        _characterSetup?.ReloadCharacterPickers();
        _profileEditor?.SetProfile(
            (HumanoidCharacterProfile?) _preferencesManager.Preferences?.SelectedCharacter,
            _preferencesManager.Preferences?.SelectedCharacterIndex);
    }

    private void OpenCharacterSavePanel()
    {
        var savePanel = new OldCharacterSetupGuiSavePanel();

        savePanel.SaveButton.OnPressed += _ =>
        {
            SaveCharacterProfile();
            savePanel.Close();
            CloseCharacterSetup();
        };

        savePanel.NoSaveButton.OnPressed += _ =>
        {
            savePanel.Close();
            CloseCharacterSetup();
        };

        savePanel.OpenCentered();
    }
}
