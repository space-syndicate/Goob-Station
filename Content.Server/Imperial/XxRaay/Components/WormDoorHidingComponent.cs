using Content.Server.Imperial.XxRaay.Systems;

namespace Content.Server.Imperial.XxRaay.Components;

[RegisterComponent]
[Access(typeof(WormDoorHideSystem))]
public sealed partial class WormDoorHidingComponent : Component
{
    [DataField]
    public EntityUid SourceDoor;

    [DataField]
    public bool WasCollidable = true;

    [DataField]
    public bool AddedStealth;

    [DataField]
    public bool PreviousStealthEnabled;

    [DataField]
    public float PreviousStealthVisibility = 1f;

    [DataField]
    public bool IgnoreNextMove;

    [DataField]
    public bool AppliedVisibility;

    [DataField]
    public bool AddedVisibility;

    [DataField]
    public ushort PreviousVisibilityLayer;
}
