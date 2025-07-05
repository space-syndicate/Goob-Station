namespace Content.Server.Cargo.Components;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>

[Flags]
public enum BuySellType : byte
{
    Buy = 1 << 0,
    Sell = 1 << 1,
    All = Buy | Sell
}


[RegisterComponent]
public sealed partial class CargoPalletComponent : Component
{
    /// <summary>
    /// Whether the pad is a buy pad, a sell pad, or all.
    /// </summary>
    [DataField]
    public BuySellType PalletType;

    //Imperial Space Pirates: New Horizon; Start

    /// <summary>
    /// Wherether the pad is able to be used to sell blacklisted entities (e.g. high risks, mobs, nuke, etc.)
    /// </summary>
    [DataField]
    public bool AvoidSellBlacklist = false;

    //Imperial Space Pirates: New Horizon; End
}
