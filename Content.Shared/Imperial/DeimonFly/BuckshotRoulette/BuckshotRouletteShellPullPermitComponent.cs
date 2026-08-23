namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Хранит количество патронов, которые игрок может извлечь благодаря выпитому пиву.
/// </summary>
[RegisterComponent]
public sealed partial class BuckshotRouletteShellPullPermitComponent : Component
{
    [DataField]
    public int Charges;
}
