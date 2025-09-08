using System;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.FactionShop;

/// <summary>
/// Компонент для магазина фракций
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FactionShopComponent : Component
{
    /// <summary>
    /// Фракция этого магазина
    /// </summary>
    [DataField]
    public string Faction = "NeutralFaction";

    /// <summary>
    /// Интервал начисления очков в секундах
    /// </summary>
    [DataField]
    public float PointsInterval = 20.0f;

    /// <summary>
    /// Количество очков за каждый флаг
    /// </summary>
    [DataField]
    public int PointsPerFlag = 10;

    /// <summary>
    /// Последнее время начисления очков
    /// </summary>
    public TimeSpan LastPointsTime = TimeSpan.Zero;
}
