using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

using Robust.Shared.GameStates;
using Content.Shared.Imperial.XxRaay.DataDefinitions;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class VentCrawlingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid SourceVent;

    [DataField, AutoNetworkedField]
    public bool RemovedComplexInteraction;

    [DataField, AutoNetworkedField]
    public bool WasCollidable = true;

    [DataField, AutoNetworkedField]
    public List<ImperialVentCrawlerFixtureState> FixtureStates = [];

    [DataField, AutoNetworkedField]
    public bool AddedStealth;

    [DataField, AutoNetworkedField]
    public bool AddedVisibility;

    [DataField, AutoNetworkedField]
    public bool PreviousStealthEnabled;

    [DataField, AutoNetworkedField]
    public float PreviousStealthVisibility = 1f;

    [DataField, AutoNetworkedField]
    public ushort PreviousVisibilityLayer;

    [DataField, AutoNetworkedField]
    public float SoundDistance;

    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> DisabledActions = [];
}

