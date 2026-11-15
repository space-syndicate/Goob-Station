using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._BlackM.PersonalObjectives;

[RegisterComponent]
public sealed partial class BlackMCounterConditionComponent : Component
{
    [DataField(required: true)]
    public string Counter = string.Empty;

    [DataField]
    public int Target = 1;

    [DataField]
    public bool RequireAlive;
}

[RegisterComponent]
public sealed partial class BlackMGlobalCounterConditionComponent : Component
{
    [DataField(required: true)]
    public string Counter = string.Empty;

    [DataField]
    public int Target = 1;

    [DataField]
    public bool Invert;

    [DataField]
    public bool RequireAlive;
}

[RegisterComponent]
public sealed partial class BlackMDepartmentSurvivalConditionComponent : Component
{
    [DataField]
    public ProtoId<DepartmentPrototype>? Department;

    [DataField]
    public float Ratio = 0.5f;

    [DataField]
    public bool RequireAlive;
}

[RegisterComponent]
public sealed partial class BlackMStayHumanConditionComponent : Component;

[RegisterComponent]
public sealed partial class BlackMEvacuateConditionComponent : Component;

[RegisterComponent]
public sealed partial class BlackMResearchConditionComponent : Component
{
    [DataField]
    public int Technologies = 10;
}

[RegisterComponent]
public sealed partial class BlackMBankBalanceConditionComponent : Component
{
    [DataField]
    public int Balance = 15000;
}
