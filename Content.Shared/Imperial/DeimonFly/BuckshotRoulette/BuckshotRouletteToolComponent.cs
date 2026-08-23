namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Задаёт одноразовый эффект предмета Buckshot Roulette.
/// </summary>
[RegisterComponent]
public sealed partial class BuckshotRouletteToolComponent : Component
{
    [DataField(required: true)]
    public BuckshotRouletteToolEffect Effect;
}
