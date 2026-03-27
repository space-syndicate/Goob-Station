using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Компонент для автоматического добавления встроенных инструментов смешера в руки.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmasherInnateToolsComponent : Component
{
    [DataField]
    public List<EntProtoId> Tools = new()
    {
        "XxRaayAutoShotgun",
        "XxRaayChainGun",
        "XxRaayShoulderRocketLauncher"
    };

    [DataField]
    public List<string> Hands = new()
    {
        "hand_right1",
        "hand_right2",
        "hand_left1"
        };

    public List<EntityUid> ToolUids = new();
}

