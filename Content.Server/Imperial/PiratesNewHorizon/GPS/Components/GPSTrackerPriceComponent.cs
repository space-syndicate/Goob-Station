using Robust.Shared.Audio;
using Content.Shared.Storage;
using Content.Server.Cargo;


namespace Content.Server.Imperial.PiratesNewHorizon.GPS.Components;
/// <summary>
/// This is used for pricing valuable items, when "GPS tracker" is removed out of them
/// Used not to change the default price of such things as high-risk, but increasing the price with GPS tracker remover
/// (Not to encourage random ppl just stealing highrisks for the sake of money, only allowing pirates to do such a profitable thing)
/// </summary>
[RegisterComponent]
public sealed partial class GPSTrackerPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on when GPS tracker is still in
    /// </summary>
    [DataField("startprice", required: true)]
    public double StartPrice;
    /// <summary>
    ///  Exact opposite, the price, when GPS tracker is out
    /// </summary>
    [DataField("endprice", required: true)]

    public double EndPrice;

    /// <summary>
    ///  Wherether the tracker is installed or not
    /// </summary>
    [DataField("gpstrackerinstalled")]
    public bool GPSTrackerInstalled = true;
}
