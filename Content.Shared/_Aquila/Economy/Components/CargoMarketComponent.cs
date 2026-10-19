using Content.Shared._Aquila.Economy.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Economy.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCargoMarketSystem))]
public sealed partial class CargoMarketComponent : Component
{
    /// <summary>
    /// Сколько кредитов станция выручила с продаж за смену. Чем больше выручка, тем дороже заказы.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Earned;

    /// <summary>
    /// Выручка за смену, при которой заказы дорожают на половину предельной наценки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int InflationScale = 50000;

    /// <summary>
    /// Предельная наценка на заказы, доля от базовой цены.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxInflation = 1f;

    /// <summary>
    /// Объём недавних продаж каждого товара в кредитах. Ключ товара: газ для газовых баллонов и канистр, тип стопки для стопок, иначе прототип.
    /// </summary>
    [DataField]
    public Dictionary<string, double> Saturation = new();

    /// <summary>
    /// Сколько кредитов товара без категории рынок недавно скупает по полной цене.
    /// </summary>
    [DataField]
    public double GoodsFreeVolume = 10000;

    /// <summary>
    /// Объём продаж товара без категории сверх полной цены в кредитах, при котором он продаётся вдвое дешевле.
    /// </summary>
    [DataField]
    public double GoodsCapacity = 20000;

    /// <summary>
    /// За какое время насыщение рынка спадает вдвое.
    /// </summary>
    [DataField]
    public TimeSpan RecoveryHalfLife = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Когда насыщение рынка пересчитывалось в последний раз.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastRecovery;
}
