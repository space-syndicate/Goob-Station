using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialStore;

/// <summary>
///     Used to define different categories for a store.
/// </summary>
[Prototype]
public sealed partial class ImperialStoreCategoryPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("priority")]
    public int Priority { get; private set; } = 0;

    private string _name = string.Empty;
}
