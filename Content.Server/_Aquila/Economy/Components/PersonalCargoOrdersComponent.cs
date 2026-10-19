using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

[RegisterComponent, Access(typeof(PersonalCargoOrderSystem))]
public sealed partial class PersonalCargoOrdersComponent : Component
{
    /// <summary>
    /// Ящик, в котором приезжает личный заказ. Ему выдаётся доступ по ID-карте заказчика.
    /// </summary>
    [DataField]
    public EntProtoId CratePrototype = "CratePersonalOrder";

    /// <summary>
    /// Доля цены личного заказа, которая уходит в бюджет станции за доставку.
    /// </summary>
    [DataField]
    public float HandlingShare = 0.1f;

    /// <summary>
    /// Бюджет, который получает плату за доставку личных заказов.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> HandlingAccount = "Cargo";

    /// <summary>
    /// Запись в истории счёта при оплате личного заказа.
    /// </summary>
    [DataField]
    public LocId PaymentReason = "bank-transaction-cargo-order";
}
