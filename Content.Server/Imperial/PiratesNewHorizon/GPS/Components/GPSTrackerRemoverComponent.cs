using Robust.Shared.Audio;
using Content.Shared.Storage;
using Content.Server.Cargo;


namespace Content.Server.Imperial.PiratesNewHorizon.GPS.Components;


/// <summary>
/// This is used for GPS Tracker Remover, a tool that increases the price of valuables.
/// </summary>
[RegisterComponent]
public sealed partial class GPSTrackerRemoverComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 10f;

    [DataField("useSound")]
    public SoundSpecifier UseSound { get; set; } = new SoundPathSpecifier("/Audio/Imperial/PiratesNewHorizon/weak_bang.ogg");

    /// <summary>
    /// The entity to spawn when GPS tracker is extracted
    /// </summary>

    [DataField("itemsToSpawn")]
    public List<EntitySpawnEntry> ItemsToSpawn = new();
}
