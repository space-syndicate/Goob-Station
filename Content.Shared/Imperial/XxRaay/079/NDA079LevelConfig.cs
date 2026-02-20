using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Конфигурация способности мерцания света NDA079 для конкретного уровня CPU.
/// </summary>
[Prototype("nda079LightFlickerLevel")]
public sealed partial class NDA079LightFlickerLevelPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField(required: true)]
    public int Level;

    [DataField(required: true)]
    public TimeSpan LightOffDuration;

    [DataField(required: true)]
    public float Radius;

    [DataField(required: true)]
    public float SuccessChance;

    [DataField(required: true)]
    public TimeSpan Cooldown;
}

[Prototype("nda079AirlockLevel")]
public sealed partial class NDA079AirlockLevelPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField(required: true)]
    public int Level;

    [DataField(required: true)]
    public TimeSpan BoltDuration;

    [DataField(required: true)]
    public float SuccessChance;
}


