namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Позволяет предметам Buckshot Roulette отличать боевой патрон от холостого.
/// </summary>
[RegisterComponent]
public sealed partial class BuckshotRouletteShellComponent : Component
{
    [DataField]
    public bool Live;
}
