using Content.Shared._Aquila.Economy.Components;

namespace Content.Shared._Aquila.Economy.Systems;

public abstract class SharedCargoMarketSystem : EntitySystem
{
    /// <summary>
    /// Возвращает, на какую долю от базовой цены подорожали заказы из-за выручки станции за смену.
    /// </summary>
    public float GetInflation(Entity<CargoMarketComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false) || station.Comp.Earned <= 0)
            return 0f;

        var earned = (float) station.Comp.Earned;
        return station.Comp.MaxInflation * earned / (earned + station.Comp.InflationScale);
    }

    /// <summary>
    /// Возвращает надбавку к цене товара карго из-за роста цен.
    /// </summary>
    public int GetMarkup(Entity<CargoMarketComponent?> station, int cost)
    {
        if (!Resolve(station, ref station.Comp, false))
            return 0;

        return (int) MathF.Round(cost * GetInflation(station));
    }
}
