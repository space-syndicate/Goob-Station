using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

public sealed partial class JobPrototype
{
    /// <summary>
    ///     Whether this job should only appear when MRP jobs are enabled.
    /// </summary>
    [DataField("mrp")]
    public bool Mrp { get; private set; } = false;
}
