using Content.Server.Imperial.EnergyCore;

namespace Content.Server.Imperial.EnergyCore.Components;
/// <summary>
/// Необходим для вывода информации касательно энерго ядра в конце раунда в том случае, если оно было перегрето
/// </summary>
[RegisterComponent]
[Access(typeof(CoreTechnicalRuleSystem))]
public sealed partial class CoreTechnicalRuleComponent : Component
{
    [DataField]
    public string EndRoundText = "endround-core-was-compromised";
}
