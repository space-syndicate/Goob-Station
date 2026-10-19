using Content.Shared.Access;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.DataDefinitions;

[DataDefinition]
public sealed partial class BankAuthority
{
    /// <summary>
    /// Доступ ID-карты, который даёт право выписывать премии и штрафы. Без значения подходит любая карта.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype>? Access;

    /// <summary>
    /// Должность на ID-карте, которая даёт право выписывать премии и штрафы. Без значения подходит любая должность.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Бюджет станции, из которого платятся премии и куда идут штрафы.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CargoAccountPrototype> Budget;

    /// <summary>
    /// Сколько всего премий можно выписать за смену без подтверждения.
    /// </summary>
    [DataField]
    public int BonusLimit = 300;

    /// <summary>
    /// Наибольший размер одного штрафа без подтверждения.
    /// </summary>
    [DataField]
    public int MaxFine = 200;

    /// <summary>
    /// Штрафы любого размера выписываются без подтверждения.
    /// </summary>
    [DataField]
    public bool UnlimitedFine;

    /// <summary>
    /// Может отменять выписанные штрафы.
    /// </summary>
    [DataField]
    public bool RevokeFines;
}
