using System.Linq;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Contraband;
using Content.Shared.Emag.Components;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy.Systems;

public abstract class SharedPaidVendingMachineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaidVendingMachineComponent, VendingMachineAuthorizeEvent>(OnVendingMachineAuthorize);
    }

    private void OnVendingMachineAuthorize(Entity<PaidVendingMachineComponent> ent, ref VendingMachineAuthorizeEvent args)
    {
        if (ent.Comp.FreeWithAccess)
            args.Authorized = true;
    }

    /// <summary>
    /// Цена товара в автомате. Ноль, если автомат бесплатный.
    /// </summary>
    public int GetPrice(Entity<PaidVendingMachineComponent?> ent, EntProtoId item)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        if (ent.Comp.PriceOverrides.TryGetValue(item, out var overridePrice))
            return overridePrice;

        if (!_proto.TryIndex(item, out var itemProto)
            || (GetListedPrice(itemProto) ?? GetCategory(itemProto)?.Price) is not { } price)
            return 0;

        return (int) Math.Round(price * ent.Comp.PriceMultiplier);
    }

    /// <summary>
    /// Цена предмета из прайс-листов: своя или ближайшего предка. Null, если её нигде нет.
    /// </summary>
    public int? GetListedPrice(EntityPrototype item)
    {
        foreach (var (id, _) in _proto.EnumerateAllParents<EntityPrototype>(item.ID, includeSelf: true))
        {
            foreach (var list in _proto.EnumeratePrototypes<VendingPriceListPrototype>())
            {
                if (list.Prices.TryGetValue(id, out var price))
                    return price;
            }
        }

        return null;
    }

    /// <summary>
    /// Цена товара для конкретного покупателя. Ноль, если ему положено бесплатно.
    /// </summary>
    public int GetPriceFor(Entity<PaidVendingMachineComponent?> ent, EntityUid user, EntProtoId item)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_economyZone.IsEconomyActive(ent) || IsFreeFor((ent, ent.Comp), user))
            return 0;

        return GetPrice(ent, item);
    }

    /// <summary>
    /// Ценовая категория товара: первая подходящая по приоритету.
    /// </summary>
    public VendingPriceCategoryPrototype? GetCategory(EntityPrototype item)
    {
        return _proto.EnumeratePrototypes<VendingPriceCategoryPrototype>()
            .OrderByDescending(category => category.Priority)
            .ThenBy(category => category.ID)
            .FirstOrDefault(category => Matches(category, item));
    }

    private bool Matches(VendingPriceCategoryPrototype category, EntityPrototype item)
    {
        if (category.Components.Any(name => !item.Components.ContainsKey(name)))
            return false;

        if (category.Tags.Count > 0
            && (!item.TryGetComponent<TagComponent>(out var tags, Factory) || !category.Tags.Any(tags.Tags.Contains)))
            return false;

        return category.Slots == SlotFlags.NONE
               || item.TryGetComponent<ClothingComponent>(out var clothing, Factory) && (clothing.Slots & category.Slots) != 0;
    }

    /// <summary>
    /// Может ли покупатель купить товар. Служебные товары из обычного ассортимента продаются только тем, кому они разрешены, или с доступом к автомату.
    /// </summary>
    public bool CanBuy(Entity<PaidVendingMachineComponent?> ent, EntityUid user, EntProtoId item, InventoryType type)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || !_economyZone.IsEconomyActive(ent)
            || type != InventoryType.Regular
            || HasComp<EmaggedComponent>(ent)
            || HasMachineAccess(ent.Owner, user))
            return true;

        if (!_proto.TryIndex(item, out var itemProto)
            || !itemProto.TryGetComponent<ContrabandComponent>(out var contraband, Factory)
            || !ent.Comp.RestrictedSeverities.Contains(contraband.Severity))
            return true;

        if (!_idCard.TryFindIdCard(user, out var idCard))
            return false;

        var allowedDepartments = contraband.AllowedDepartments;
        var allowedJobs = contraband.AllowedJobs;
        return idCard.Comp.JobDepartments.Any(allowedDepartments.Contains)
               || idCard.Comp.JobPrototype is { } job && allowedJobs.Contains(job);
    }

    private bool IsFreeFor(Entity<PaidVendingMachineComponent> ent, EntityUid user)
    {
        if (HasComp<EmaggedComponent>(ent)
            || ent.Comp.FreeWithAccess && HasMachineAccess(ent, user))
            return true;

        return ent.Comp.FreeAccess.Count > 0
               && _accessReader.FindAccessTags(user).Any(tag => ent.Comp.FreeAccess.Contains(tag));
    }

    private bool HasMachineAccess(EntityUid machine, EntityUid user)
    {
        return TryComp<AccessReaderComponent>(machine, out var reader) && _accessReader.IsAllowed(user, machine, reader);
    }
}
