using Content.Server.Imperial.XxRaay.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Imperial.XxRaay.Components;

[RegisterComponent]
[Access(typeof(WormDoorHideSystem))]
public sealed partial class WormDoorHidingComponent : Component
{
    [ViewVariables]
    public EntityUid SourceDoor;

    [ViewVariables]
    public bool WasCollidable = true;

    [ViewVariables]
    public bool AddedStealth;

    [ViewVariables]
    public bool PreviousStealthEnabled;

    [ViewVariables]
    public float PreviousStealthVisibility = 1f;

    [ViewVariables]
    public bool IgnoreNextMove;

    [ViewVariables]
    public bool AppliedVisibility;

    [ViewVariables]
    public bool AddedVisibility;

    [ViewVariables]
    public ushort PreviousVisibilityLayer;

    [ViewVariables]
    public bool Exiting;
}
