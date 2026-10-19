using Content.Server._Aquila.Economy.DataDefinitions;
using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

[RegisterComponent, Access(typeof(BankCartridgeSystem))]
public sealed partial class StationBankManagementComponent : Component
{
    /// <summary>
    /// Премии сверх лимита и штрафы больше потолка, которые ждут подтверждения.
    /// </summary>
    [DataField]
    public List<BankManageRecord> Requests = new();

    /// <summary>
    /// Последние выписанные штрафы, которые ещё можно отменить, от старых к новым.
    /// </summary>
    [DataField]
    public List<BankManageRecord> Fines = new();

    /// <summary>
    /// Сколько последних штрафов можно отменить.
    /// </summary>
    [DataField]
    public int MaxFines = 20;

    /// <summary>
    /// Номер следующей записи.
    /// </summary>
    [DataField]
    public int NextId = 1;

    /// <summary>
    /// Описание премии в истории операций.
    /// </summary>
    [DataField]
    public LocId BonusReason = "bank-transaction-bonus";

    /// <summary>
    /// Описание штрафа в истории операций.
    /// </summary>
    [DataField]
    public LocId FineReason = "bank-transaction-fine";

    /// <summary>
    /// Описание возврата отменённого штрафа в истории операций.
    /// </summary>
    [DataField]
    public LocId FineRevokedReason = "bank-transaction-fine-revoked";

    /// <summary>
    /// Канал рации, в котором объявляются запросы на подтверждение премий и штрафов.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RequestChannel = "Command";
}
