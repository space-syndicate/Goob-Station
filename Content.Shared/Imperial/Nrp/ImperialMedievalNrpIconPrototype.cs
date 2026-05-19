using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.StatusIcon;

/// <summary>
/// StatusIcons for Imperial Medieval NRP
/// </summary>
[Prototype]
public sealed partial class NrpIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<NrpIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
