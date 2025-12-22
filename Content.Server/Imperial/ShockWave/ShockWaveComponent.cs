using Content.Shared.EntityEffects;

namespace Content.Server.Imperial.ShockWave;


[RegisterComponent]
public sealed partial class ShockWaveComponent : Component
{
    [DataField]
    public LookupFlags CollideFlags = LookupFlags.All;

    [DataField]
    public float Speed = 1;

    [DataField]
    public float BorderWidth = 0.0f;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(0);

    /// <summary>
    /// List of effects that should be applied.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];


    [ViewVariables]
    public TimeSpan NextUpdate = TimeSpan.FromSeconds(0);

    [ViewVariables]
    public TimeSpan SpawnTime = TimeSpan.FromSeconds(0);

    [ViewVariables]
    public List<EntityUid> CollidedEntities = new();
}
