using Content.Server.Imperial.XxRaay.Systems;
using Content.Server.Imperial.XxRaay.DataDefinitions;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.Imperial.XxRaay.Components;

[RegisterComponent]
[Access(typeof(ImperialVentCrawlerSystem))]
public sealed partial class VentCrawlingComponent : Component
{
    [DataField]
    public EntityUid SourceVent;

    [DataField]
    public bool RemovedComplexInteraction;

    [DataField]
    public bool WasCollidable = true;

    [DataField]
    public List<ImperialVentCrawlerFixtureState> FixtureStates = [];

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

    [ViewVariables]
    public HashSet<EntityUid> DisabledActions = [];
}
