using CVars = Content.Shared._CorvaxGoob.CCCVars.CCCVars;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Jobs;

public sealed partial class MrpJobSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        RemoveMrpJobPreferences();
        InitializeMrpSystem();
    }

    private void RemoveMrpJobPreferences()
    {
        if (_cfg.GetCVar(CVars.MrpJobsEnabled))
            return;

        foreach (var department in _prototypes.EnumeratePrototypes<DepartmentPrototype>())
        {
            department.Roles.RemoveAll(jobId =>
                _prototypes.TryIndex(jobId, out JobPrototype? proto) && proto.Mrp);
        }
    }

    partial void InitializeMrpSystem();
}
