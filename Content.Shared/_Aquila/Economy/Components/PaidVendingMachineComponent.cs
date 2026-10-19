using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Contraband;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedPaidVendingMachineSystem))]
public sealed partial class PaidVendingMachineComponent : Component
{
    /// <summary>
    /// Множитель цен категорий для этого автомата.
    /// </summary>
    [DataField]
    public float PriceMultiplier = 1f;

    /// <summary>
    /// Цены, заданные вручную вместо цены категории.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> PriceOverrides = new();

    /// <summary>
    /// Превращает ограничение доступа автомата в бесплатность: с доступом товары бесплатные, без доступа их можно купить.
    /// </summary>
    [DataField]
    public bool FreeWithAccess = true;

    /// <summary>
    /// Доступы, с любым из которых товары бесплатные. Подходит для автоматов без ограничения доступа.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> FreeAccess = new();

    /// <summary>
    /// Уровни контрабанды, которые из обычного ассортимента продаются только тем, кому они разрешены, или с доступом к автомату.
    /// </summary>
    [DataField]
    public List<ProtoId<ContrabandSeverityPrototype>> RestrictedSeverities = new() { "Restricted", "Major", "GrandTheft", "HighlyIllegal", "Syndicate" };

    /// <summary>
    /// Доля оплаты, которая идёт в бюджет станции. Остальное уходит ЦК.
    /// </summary>
    [DataField]
    public float RevenueShare = 0.2f;

    /// <summary>
    /// Счёт бюджета станции, куда идёт выручка.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> RevenueAccount = "Service";

    /// <summary>
    /// Описание покупки в истории операций.
    /// </summary>
    [DataField]
    public LocId PurchaseReason = "bank-transaction-purchase";

    /// <summary>
    /// Звук успешной оплаты, который слышит покупатель.
    /// </summary>
    [DataField]
    public SoundSpecifier? PurchaseSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_success.ogg", AudioParams.Default.WithVolume(-7f));
}
