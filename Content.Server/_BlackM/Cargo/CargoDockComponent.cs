using Content.Shared._BlackM.Cargo;
namespace Content.Server._BlackM.Cargo;

public enum CargoDockMode : byte { Import, Export }

[RegisterComponent]
public sealed partial class CargoDockComponent : Component
{
    [DataField] public CargoDockMode Mode;
    [DataField] public int QueueCapacity = 32;
    [DataField] public float Cooldown = 1f;
    // 0 = one per pad tile.
    [DataField] public int MaxItems;
    [DataField] public bool FallThrough = true;
    public EntityUid? Hood;
    public CargoState State;
    public float CooldownLeft;
    public CargoPhase VisualPhase;
}
