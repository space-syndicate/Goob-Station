using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.ErtCall;

[Prototype("ertCall")]
public sealed partial class ErtCallPresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("path")] public string Path { get; set; } = string.Empty;

    [DataField("desc")] public string Desc { get; set; } = string.Empty;
}
