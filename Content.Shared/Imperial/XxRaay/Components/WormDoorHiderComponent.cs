using Content.Shared.Imperial.XxRaay;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent]
public sealed partial class WormDoorHiderComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype> HighSecDoorTag = "HighSecDoor";

    [DataField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public bool UseStealth;

    [DataField]
    public float StealthVisibility = -1f;

    [DataField]
    public ushort HiddenVisibilityLayer = XxRaayVisibilityLayers.Normal;

    [DataField]
    public ushort DoorHideVisibilityLayer = XxRaayVisibilityLayers.DoorHide;

    [DataField]
    public bool EyeSeeDoorHideLayer = true;

    [DataField(required: true)]
    public LocId EnterVerb;

    [DataField(required: true)]
    public LocId ExitVerb;

    [DataField(required: true)]
    public SpriteSpecifier EnterVerbIcon;

    [DataField(required: true)]
    public SpriteSpecifier ExitVerbIcon;

    [DataField]
    public int EnterVerbPriority;

    [DataField]
    public int ExitVerbPriority;

    [DataField]
    public bool AmbushOnOpen = true;

    [DataField]
    public float AmbushStaminaDamage = 100f;

    [DataField]
    public TimeSpan AmbushKnockdownTime = TimeSpan.FromSeconds(6);
}
