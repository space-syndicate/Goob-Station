using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Containers;
using Content.Shared.Database;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.StationRecords;
using Content.Shared.Storage.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class PersonalCargoOrderSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// Возвращает номер личного счёта, с которого покупатель может оплатить заказ на станции, или null, если личные заказы ему недоступны.
    /// </summary>
    public int? GetPayerAccount(Entity<PersonalCargoOrdersComponent?> station, EntityUid buyer)
    {
        if (!Resolve(station, ref station.Comp, false)
            || !_cfg.GetCVar(AquilaCVars.Economy)
            || !_idCard.TryFindIdCard(buyer, out var idCard)
            || !_bank.TryGetAccount(idCard.Owner, out var account))
            return null;

        return account.Value.Comp.Number;
    }

    /// <summary>
    /// Делает заказ личным: он оплачивается со счёта покупателя и, если товар помещается в ящик, приезжает в ящике под его ID-карту.
    /// </summary>
    public void MakePersonal(CargoOrderData order, int payer)
    {
        order.PersonalAccount = payer;
        order.SecuredDelivery = _proto.TryIndex<EntityPrototype>(order.ProductId, out var product) && FitsInCrate(product);
    }

    /// <summary>
    /// Проверяет, хватает ли на счёте заказчика денег на личный заказ.
    /// </summary>
    public bool CanAfford(Entity<PersonalCargoOrdersComponent?> station, CargoOrderData order, int cost)
    {
        if (!Resolve(station, ref station.Comp, false))
            return false;

        return order.PersonalAccount is { } number
               && _bank.TryFindAccount(number, out var account)
               && account.Value.Comp.Balance >= cost;
    }

    /// <summary>
    /// Списывает оплату личного заказа со счёта заказчика и перечисляет плату за доставку в бюджет станции.
    /// </summary>
    public void Pay(Entity<PersonalCargoOrdersComponent?> station, CargoOrderData order, int cost)
    {
        if (!Resolve(station, ref station.Comp, false)
            || order.PersonalAccount is not { } number
            || !_bank.TryFindAccount(number, out var account))
            return;

        _bank.Charge(account.Value.AsNullable(), cost, station.Comp.PaymentReason, order.ProductName);
        _cargo.UpdateBankAccount(station.Owner, (int) (cost * station.Comp.HandlingShare), station.Comp.HandlingAccount);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"Account {number} ({account.Value.Comp.OwnerName}) paid {cost} credits for personal cargo order {order.OrderId} ({order.ProductId})");
    }

    /// <summary>
    /// Создаёт ящик личного заказа, который открывается только ID-картой заказчика. Возвращает null, если заказ не личный или у заказчика нет записи в базе станции.
    /// </summary>
    public EntityUid? SpawnCrate(CargoOrderData order, EntityCoordinates coordinates)
    {
        if (order.PersonalAccount is not { } number
            || _station.GetOwningStation(coordinates.EntityId) is not { } station
            || !TryComp<PersonalCargoOrdersComponent>(station, out var personal)
            || !_bank.TryFindAccount(number, out var account))
            return null;

        var keys = GetRecordKeys(account.Value.Owner);
        if (keys.Count == 0)
            return null;

        var crate = Spawn(personal.CratePrototype, coordinates);
        if (TryComp<AccessReaderComponent>(crate, out var reader))
            _accessReader.SetAccessKeys((crate, reader), keys);

        return crate;
    }

    private bool FitsInCrate(EntityPrototype product)
    {
        if (!product.HasComponent<StorageFillComponent>(Factory) && !product.HasComponent<EntityTableContainerFillComponent>(Factory))
            return product.HasComponent<ItemComponent>(Factory);

        if (!product.TryGetComponent<AccessReaderComponent>(out var reader, Factory))
            return true;

        var accessLists = reader.AccessLists;
        return accessLists.Count == 0;
    }

    private HashSet<StationRecordKey> GetRecordKeys(EntityUid account)
    {
        var keys = new HashSet<StationRecordKey>();
        var query = EntityQueryEnumerator<BankCardComponent, StationRecordKeyStorageComponent>();
        while (query.MoveNext(out var card, out var storage))
        {
            if (card.Account == account && storage.Key is { } key)
                keys.Add(key);
        }

        return keys;
    }
}
