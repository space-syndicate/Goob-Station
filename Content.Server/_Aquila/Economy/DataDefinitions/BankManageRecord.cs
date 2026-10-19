using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.DataDefinitions;

[DataDefinition]
public sealed partial class BankManageRecord
{
    /// <summary>
    /// Номер записи, по которому её подтверждают или отменяют.
    /// </summary>
    [DataField]
    public int Id;

    /// <summary>
    /// Штраф, а не премия.
    /// </summary>
    [DataField]
    public bool Fine;

    /// <summary>
    /// Счёт того, кто выписал премию или штраф.
    /// </summary>
    [DataField]
    public EntityUid Issuer;

    /// <summary>
    /// Счёт получателя премии или нарушителя.
    /// </summary>
    [DataField]
    public EntityUid Target;

    /// <summary>
    /// Бюджет станции, из которого платится премия или куда идёт штраф.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> Budget;

    /// <summary>
    /// Сумма в кредитах.
    /// </summary>
    [DataField]
    public int Amount;

    /// <summary>
    /// Причина премии или штрафа.
    /// </summary>
    [DataField]
    public string Comment = string.Empty;

    /// <summary>
    /// Имя того, кто выписал премию или штраф.
    /// </summary>
    [DataField]
    public string IssuerName = string.Empty;

    /// <summary>
    /// Имя получателя премии или нарушителя.
    /// </summary>
    [DataField]
    public string TargetName = string.Empty;
}
