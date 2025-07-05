using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Input;
using Content.Client.Popups;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Imperial.ImperialBorgs;
using Content.Shared.Imperial.ImperialBorgs.Events;
using Robust.Client.Input;

namespace Content.Client.Imperial.ImperialBorgs;

[UsedImplicitly]
public sealed class BorgHypoUIController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IEntityNetworkManager _net = null!;
    [Dependency] private readonly IInputManager _input = default!;

    private SimpleRadialMenu? _menu;
    private EntityUid? _activeHypo;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenBorgHypoUIEvent>(OnOpenUI);
        
        //_input.SetInputCommand(ContentKeyFunctions.OpenEmotesMenu,
        //  InputCmdHandler.FromDelegate(_ => ToggleMenu()));
    }

    private RadialMenuOption ImperialBorgsRadialOption(ReagentPrototype proto, ImperialBorgsReagent reagent)
    {
        var spritePath = reagent.Sprite ?? new ResPath("/Textures/Interface/Misc/beakerlarge.png");

        var option = new RadialMenuActionOption<ReagentPrototype>(HandleRadialButtonClick, proto)
        {
            Sprite = new SpriteSpecifier.Texture(spritePath),
            ToolTip = Loc.GetString(proto.LocalizedName)
        };
        return option;
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<BorgHypoUIController>();
    }

    private void OnOpenUI(OpenBorgHypoUIEvent ev, EntitySessionEventArgs args)
    {
        var uid = _entityManager.GetEntity(ev.Entity);
        if (!_entityManager.TryGetComponent<BorgHypoComponent>(uid, out var hypo))
            return;

        _activeHypo = uid;
        OpenMenu(hypo);
    }

    private void OpenMenu(BorgHypoComponent hypo)
    {
        CloseMenu();
        var models = ConvertToButtons(hypo.Solutions).ToList();

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(models);
        _menu.Open();
        _menu.OpenCentered();
    }

    private void ToggleMenu()
    {
        if (_menu == null)
        {
            var player = _playerManager.LocalSession?.AttachedEntity;
            if (player == null || !_entityManager.TryGetComponent<BorgHypoComponent>(player.Value, out var hypo))
                return;

            OpenMenu(hypo);
        }
        else
        {
            CloseMenu();
        }
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Close();
        _menu = null;
    }

    private IEnumerable<RadialMenuOption> ConvertToButtons(List<BorgHypoSolution> solutions)
    {
        var models = new List<RadialMenuOption>();

        foreach (var solution in solutions)
        {
            if (solution.Reagents.Count == 0)
                continue;

            var reagent = solution.Reagents[0];
            if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                continue;

            var option = ImperialBorgsRadialOption(proto, reagent);
            models.Add(option);
        }

        return models;
    }

    private void HandleRadialButtonClick(ReagentPrototype prototype)
    {
        if (_activeHypo == null || !_entityManager.TryGetComponent<BorgHypoComponent>(_activeHypo.Value, out _))
        {
            return;
        }

        var netEntity = _entityManager.GetNetEntity(_activeHypo.Value);
        var msg = new ChangeReagentEvent(prototype.ID, netEntity);
        _net.SendSystemNetworkMessage(msg);

        var popup = _entityManager.System<PopupSystem>();
        popup.PopupClient(Loc.GetString("borghypo-ui-controller-change-reagent-popup", ("reagent", prototype.LocalizedName)),
            _activeHypo.Value,
            _playerManager.LocalSession?.AttachedEntity);

        CloseMenu();
    }
}
