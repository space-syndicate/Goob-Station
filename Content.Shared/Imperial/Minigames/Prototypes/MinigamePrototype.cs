using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Minigames;


[Prototype]
public sealed partial class MinigamePrototype : MinigameData, IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;
}
