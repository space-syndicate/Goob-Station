namespace Content.Shared.Imperial.MiningWeapons.Smasher;

/// <summary>
/// Is needed to properly monitor the charging status on the server.
/// </summary>
public sealed class ChargeData
{
    public TimeSpan StartTime { get; set; }
    public EntityUid User { get; set; }
}
