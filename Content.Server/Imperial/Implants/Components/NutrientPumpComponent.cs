namespace Content.Server.Imperial.Implants.Components;

[RegisterComponent]
public sealed partial class NutrimentPumpComponent : Component
{

    [DataField] public bool HadThirst = false;


    [DataField] public bool HadHunger = false;
}
