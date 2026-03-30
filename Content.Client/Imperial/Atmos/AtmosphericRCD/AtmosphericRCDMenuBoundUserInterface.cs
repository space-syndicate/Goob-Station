using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared.Imperial.Atmospheric.RCD;
using Content.Shared.Imperial.Atmospheric.RCD.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Imperial.Atmospheric.RCD;

[UsedImplicitly]
public sealed class AtmosphericRCDMenuBoundUserInterface : BoundUserInterface
{
    private const string TopLevelActionCategory = "Main";

    private static readonly Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)> PrototypesGroupingInfo
        = new Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)>
        {
            ["Pipes"] = ("rcd-component-pipes", new SpriteSpecifier.Texture(new ResPath("/Textures/Imperial/Gases/Tools/AtmosphericRCD/storageFourway.png"))),
            ["Unary"] = ("rcd-component-unary", new SpriteSpecifier.Texture(new ResPath("/Textures/Imperial/Gases/Tools/AtmosphericRCD/scrubber.png"))),
            ["Trinary"] = ("rcd-component-trinary", new SpriteSpecifier.Texture(new ResPath("/Textures/Imperial/Gases/Tools/AtmosphericRCD/GasPump.png"))),
            ["Filter"] = ("rcd-component-filter", new SpriteSpecifier.Texture(new ResPath("/Textures/Imperial/Gases/Tools/AtmosphericRCD/GasFilter.png"))),
            ["Hydrogen"] = ("rcd-component-hydrogen", new SpriteSpecifier.Texture(new ResPath("/Textures/Imperial/Gases/Tools/AtmosphericRCD/HydrogenGasFilter.png"))),
        };

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private SimpleRadialMenu? _menu;

    public AtmosphericRCDMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<AtmosphericRCDComponent>(Owner, out var rcd))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var models = ConvertToButtons(rcd.AvailablePrototypes);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(HashSet<ProtoId<AtmosphericRCDPrototype>> prototypes)
    {
        Dictionary<string, List<RadialMenuActionOptionBase>> buttonsByCategory = new();
        ValueList<RadialMenuActionOptionBase> topLevelActions = new();
        foreach (var protoId in prototypes)
        {
            var prototype = _prototypeManager.Index(protoId);
            if (prototype.Category == TopLevelActionCategory)
            {
                var topLevelActionOption = new RadialMenuActionOption<AtmosphericRCDPrototype>(HandleMenuOptionClick, prototype)
                {
                    IconSpecifier = RadialMenuIconSpecifier.With(prototype.Sprite),
                    ToolTip = GetTooltip(prototype)
                };
                topLevelActions.Add(topLevelActionOption);
                continue;
            }

            if (!PrototypesGroupingInfo.TryGetValue(prototype.Category, out var groupInfo))
                continue;

            if (!buttonsByCategory.TryGetValue(prototype.Category, out var list))
            {
                list = new List<RadialMenuActionOptionBase>();
                buttonsByCategory.Add(prototype.Category, list);
            }

            var actionOption = new RadialMenuActionOption<AtmosphericRCDPrototype>(HandleMenuOptionClick, prototype)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(prototype.Sprite),
                ToolTip = GetTooltip(prototype)
            };
            list.Add(actionOption);
        }

        var models = new RadialMenuOptionBase[buttonsByCategory.Count + topLevelActions.Count];
        var i = 0;
        foreach (var (key, list) in buttonsByCategory)
        {
            var groupInfo = PrototypesGroupingInfo[key];
            models[i] = new RadialMenuNestedLayerOption(list)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(groupInfo.Sprite),
                ToolTip = Loc.GetString(groupInfo.Tooltip)
            };
            i++;
        }

        foreach (var action in topLevelActions)
        {
            models[i] = action;
            i++;
        }

        return models;
    }

    private void HandleMenuOptionClick(AtmosphericRCDPrototype proto)
    {
        // A predicted message cannot be used here as the RCD UI is closed immediately
        // after this message is sent, which will stop the server from receiving it
        SendMessage(new AtmosphericRCDSystemMessage(proto.ID));


        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        var msg = Loc.GetString("atmospheric-rcd-component-change-mode", ("mode", Loc.GetString(proto.SetName)));

        if (proto.Mode is AtmosphericRcdMode.ConstructTile or AtmosphericRcdMode.ConstructObject)
        {
            var name = Loc.GetString(proto.SetName);

            if (proto.Prototype != null &&
                _prototypeManager.TryIndex(proto.Prototype, out var entProto))
                name = entProto.Name;

            msg = Loc.GetString("atmospheric-rcd-component-change-build-mode", ("name", name));
        }

        // Popup message
        var popup = EntMan.System<PopupSystem>();
        popup.PopupClient(msg, Owner, _playerManager.LocalSession.AttachedEntity);
    }

    private string GetTooltip(AtmosphericRCDPrototype proto)
    {
        string tooltip;

        if (proto.Mode is AtmosphericRcdMode.ConstructTile or AtmosphericRcdMode.ConstructObject
            && proto.Prototype != null
            && _prototypeManager.TryIndex(proto.Prototype, out var entProto))
        {
            tooltip = Loc.GetString(entProto.Name);
        }
        else
        {
            tooltip = Loc.GetString(proto.SetName);
        }

        tooltip = OopsConcat(char.ToUpper(tooltip[0]).ToString(), tooltip.Remove(0, 1));

        return tooltip;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }
}
