using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Генерирует случайный набор боевых и холостых патронов и переносит их в дробовик.
/// </summary>
[RegisterComponent]
public sealed partial class BuckshotRouletteAmmoCaseComponent : Component
{
    [DataField(required: true)]
    public EntProtoId LiveShell = string.Empty;

    [DataField(required: true)]
    public EntProtoId BlankShell = string.Empty;

    [DataField]
    public int MinimumShells = 2;

    [DataField]
    public int MaximumShells = 8;
}
