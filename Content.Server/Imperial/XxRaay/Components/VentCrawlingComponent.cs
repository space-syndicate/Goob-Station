using Content.Server.Imperial.XxRaay.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.Imperial.XxRaay.Components;

[DataDefinition]
public sealed partial class VentCrawlerFixtureState
{
    [DataField]
    public string Id = string.Empty;

    [DataField]
    public bool Hard;

    [DataField]
    public int CollisionLayer;

    [DataField]
    public int CollisionMask;
}

[RegisterComponent]
[Access(typeof(VentCrawlerSystem))]
public sealed partial class VentCrawlingComponent : Component
{
    [DataField]
    public EntityUid SourceVent;

    [DataField]
    public bool RemovedComplexInteraction;

    [DataField]
    public bool WasCollidable = true;

    [DataField]
    public List<VentCrawlerFixtureState> FixtureStates = [];

    [DataField]
    public bool AddedStealth;

    [DataField]
    public bool AddedVisibility;

    [DataField]
    public bool PreviousStealthEnabled;

    [DataField]
    public float PreviousStealthVisibility = 1f;

    [DataField]
    public ushort PreviousVisibilityLayer;

    [DataField]
    public float SoundDistance;

    public HashSet<EntityUid> DisabledActions = [];
}
