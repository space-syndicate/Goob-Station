using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class CargoMarketSystem : SharedCargoMarketSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CargoMarketComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CargoMarketComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastRecovery = _timing.CurTime;
    }

    /// <summary>
    /// Продаёт товар на рынке ЦК: возвращает выручку с учётом насыщения рынка и запоминает продажу.
    /// </summary>
    public double Sell(Entity<CargoMarketComponent?> station, EntityUid item, double price)
    {
        if (!Resolve(station, ref station.Comp, false) || !_cfg.GetCVar(AquilaCVars.Economy))
            return price;

        Recover((station, station.Comp));
        var payout = Saturate(station.Comp, item, price, station.Comp.Saturation);

        station.Comp.Earned += (int) Math.Round(payout);
        Dirty(station);
        return payout;
    }

    /// <summary>
    /// Оценивает выручку за партию товаров с учётом насыщения рынка, ничего не продавая.
    /// </summary>
    public double Appraise(Entity<CargoMarketComponent?> station, IEnumerable<(EntityUid Item, double Price)> goods)
    {
        if (!Resolve(station, ref station.Comp, false) || !_cfg.GetCVar(AquilaCVars.Economy))
            return goods.Sum(good => good.Price);

        var recovery = GetRecovery(station.Comp);
        var saturation = station.Comp.Saturation.ToDictionary(pair => pair.Key, pair => pair.Value * recovery);

        var total = 0.0;
        foreach (var (item, price) in goods)
        {
            total += Saturate(station.Comp, item, price, saturation);
        }

        return total;
    }

    private double Saturate(CargoMarketComponent market, EntityUid item, double price, Dictionary<string, double> saturation)
    {
        if (price <= 0 || _cargo.IsBountyComplete(item, out _))
            return price;

        var volumes = new Dictionary<string, double>();
        var limits = new Dictionary<string, (double Free, double Capacity)>();
        var exempt = 0.0;
        var total = CollectVolumes(market, item, volumes, limits, ref exempt);
        if (total <= 0)
            return price;

        var scale = price / total;
        var payout = exempt * scale;

        foreach (var (key, volume) in volumes)
        {
            var (free, capacity) = limits[key];
            payout += SaturateVolume(saturation, key, volume * scale, free, capacity);
        }

        return payout;
    }

    private static double SaturateVolume(Dictionary<string, double> saturation, string key, double volume, double free, double capacity)
    {
        var current = saturation.GetValueOrDefault(key);
        saturation[key] = current + volume;

        var fullPrice = Math.Clamp(free - current, 0, volume);
        var rest = volume - fullPrice;
        if (rest <= 0)
            return fullPrice;

        var over = Math.Max(0, current - free);
        return fullPrice + capacity * Math.Log(1 + rest / (capacity + over));
    }

    private double CollectVolumes(
        CargoMarketComponent market,
        EntityUid uid,
        Dictionary<string, double> volumes,
        Dictionary<string, (double Free, double Capacity)> limits,
        ref double exempt)
    {
        var total = 0.0;
        var own = _pricing.GetPrice(uid, includeContents: false);
        if (own > 0)
        {
            total += own;
            if (TryGetGood(market, uid, out var key, out var free, out var capacity))
            {
                volumes[key] = volumes.GetValueOrDefault(key) + own;
                limits[key] = (free, capacity);
            }
            else
            {
                exempt += own;
            }
        }

        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return total;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var child in container.ContainedEntities)
            {
                total += CollectVolumes(market, child, volumes, limits, ref exempt);
            }
        }

        return total;
    }

    private bool TryGetGood(CargoMarketComponent market, EntityUid uid, out string key, out double free, out double capacity)
    {
        key = string.Empty;
        free = market.GoodsFreeVolume;
        capacity = market.GoodsCapacity;

        if (GetCategory(uid) is { } category)
        {
            if (category.Capacity is not { } categoryCapacity)
                return false;

            free = category.FreeVolume;
            capacity = categoryCapacity;
        }

        if (GetGoodKey(uid) is not { } goodKey)
            return false;

        key = goodKey;
        return true;
    }

    private string? GetGoodKey(EntityUid uid)
    {
        string? gas = null;
        if (TryComp<GasCanisterComponent>(uid, out var canister))
            gas = GetDominantGas(canister.Air);
        else if (TryComp<GasTankComponent>(uid, out var tank))
            gas = GetDominantGas(tank.Air);

        if (gas != null)
            return gas;

        if (TryComp<StackComponent>(uid, out var stack))
            return stack.StackTypeId.Id;

        return MetaData(uid).EntityPrototype?.ID;
    }

    private static string? GetDominantGas(GasMixture air)
    {
        var dominant = -1;
        var most = 0f;
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            if (air[i] <= most)
                continue;

            most = air[i];
            dominant = i;
        }

        return dominant < 0 ? null : ((Gas) dominant).ToString();
    }

    private CargoMarketCategoryPrototype? GetCategory(EntityUid uid)
    {
        CargoMarketCategoryPrototype? best = null;
        foreach (var category in _proto.EnumeratePrototypes<CargoMarketCategoryPrototype>())
        {
            if ((best != null && category.Priority <= best.Priority) || !IsInCategory(uid, category))
                continue;

            best = category;
        }

        return best;
    }

    private bool IsInCategory(EntityUid uid, CargoMarketCategoryPrototype category)
    {
        foreach (var name in category.Components)
        {
            if (Factory.TryGetRegistration(name, out var registration) && HasComp(uid, registration.Type))
                return true;
        }

        return _tag.HasAnyTag(uid, category.Tags);
    }

    private void Recover(Entity<CargoMarketComponent> market)
    {
        var recovery = GetRecovery(market.Comp);
        market.Comp.LastRecovery = _timing.CurTime;

        foreach (var key in market.Comp.Saturation.Keys.ToList())
        {
            market.Comp.Saturation[key] *= recovery;
        }
    }

    private double GetRecovery(CargoMarketComponent market)
    {
        return Math.Pow(0.5, (_timing.CurTime - market.LastRecovery) / market.RecoveryHalfLife);
    }
}
