using Content.Server.Imperial.XxRaay.Systems;

namespace Content.Server.Imperial.XxRaay.Components;

[RegisterComponent]
[Access(typeof(WormDoorHideSystem))]
public sealed partial class WormDoorHidingComponent : Component
{
    public EntityUid SourceDoor;

    public bool WasCollidable = true;

    public bool AddedStealth;

    public bool PreviousStealthEnabled;

    public float PreviousStealthVisibility = 1f;

    public bool IgnoreNextMove;

    public bool AppliedVisibility;

    public bool AddedVisibility;

    public ushort PreviousVisibilityLayer;
}
