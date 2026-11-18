using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._BlackM.Rappel;

[RegisterComponent]
public sealed partial class RappelConsoleComponent : Component
{
    [DataField]
    public string Channel = "default";

    [DataField]
    public string ZoneId = "default";

    [DataField]
    public float CameraZoom = 1f;

    [DataField]
    public float MaxZoom = 1.5f;

    [ViewVariables]
    public EntityUid? Camera;

    [ViewVariables]
    public HashSet<EntityUid> Viewers = new();
}

[RegisterComponent]
public sealed partial class RappelZoneComponent : Component
{
    [DataField]
    public string Id = "default";

    [DataField]
    public LocId? ZoneName;

    [DataField]
    public float Radius = 12f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RappelRopeComponent : Component
{
    [DataField]
    public string Channel = "default";

    [DataField, AutoNetworkedField]
    public bool Linked;

    [DataField]
    public TimeSpan HookDelay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public float DescentTime = 3.5f;

    [DataField]
    public float RopeDeployTime = 1.2f;

    [DataField]
    public bool ThrowFlashbang = true;

    [DataField]
    public int FlashbangCount = 3;

    [DataField]
    public float FlashbangDistance = 2.5f;

    [DataField]
    public float DropHeight = 7f;

    [DataField]
    public float SlotSpacing = 1.1f;

    [DataField]
    public float SlotHoldTime = 4f;

    [DataField]
    public SoundSpecifier? HookSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg");

    [DataField]
    public SoundSpecifier? DescentSound = new SoundPathSpecifier("/Audio/Items/zip.ogg");

    [DataField]
    public bool LoopDescentSound;

    [DataField]
    public SoundSpecifier? UnhookSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_end.ogg");

    [DataField]
    public SoundSpecifier? LandSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public bool CeilingCollapse = true;

    [DataField]
    public SoundSpecifier? CeilingCollapseSound = new SoundPathSpecifier("/Audio/Effects/explosion_small1.ogg");

    [DataField]
    public List<EntProtoId> CeilingDebris = new()
    {
        "BlackMRappelDebrisCeilingTile",
        "BlackMRappelDebrisRubble",
        "BlackMRappelDebrisBricks",
        "BlackMRappelDebrisScrap",
        "BlackMRappelDebrisCables",
    };

    [DataField]
    public int CeilingDebrisCount;

    [DataField]
    public List<EntProtoId> CeilingSmallDebris = new()
    {
        "BlackMRappelDebrisShard1",
        "BlackMRappelDebrisShard2",
        "BlackMRappelDebrisShard3",
        "BlackMRappelDebrisPebble1",
        "BlackMRappelDebrisPebble2",
    };

    [DataField]
    public int CeilingSmallDebrisCount;

    [DataField]
    public EntProtoId CeilingDust = "BlackMRappelCeilingDust";

    [DataField]
    public int CeilingDustCount = 5;

    [DataField]
    public float CeilingDebrisRadius = 1.3f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RappellingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Preparing;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField, AutoNetworkedField]
    public float Duration = 3.5f;

    [DataField, AutoNetworkedField]
    public float Height = 7f;

    [DataField, AutoNetworkedField]
    public float SwaySeed;

    [ViewVariables]
    public SoundSpecifier? LandSound;

    [ViewVariables]
    public Vector2 OriginalOffset;

    [ViewVariables]
    public bool OffsetSaved;

    [ViewVariables]
    public Angle OriginalRotation;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RappelDeployedRopeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DeployStart;

    [DataField, AutoNetworkedField]
    public float DeployDuration = 1.2f;

    [DataField, AutoNetworkedField]
    public float Height = 7f;

    [DataField, AutoNetworkedField]
    public bool InUse;

    [DataField, AutoNetworkedField]
    public bool Landed;
}

[RegisterComponent]
public sealed partial class RappelBallastComponent : Component
{
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RappelCeilingBreachComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField]
    public float Duration = 6f;

    [DataField]
    public float ShakeRadius = 8f;

    [DataField]
    public float ShakeStrength = 0.45f;

    [DataField]
    public float ShakeDuration = 0.8f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RappelFallingDebrisComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField, AutoNetworkedField]
    public float FallHeight = 7f;

    [DataField, AutoNetworkedField]
    public float FallTime = 0.75f;

    [DataField, AutoNetworkedField]
    public float Spin;

    [DataField, AutoNetworkedField]
    public Angle FinalRotation;

    [DataField, AutoNetworkedField]
    public float Drift;

    [DataField, AutoNetworkedField]
    public float ScaleMultiplier = 1f;

    [DataField]
    public float Scale = 1f;

    [DataField]
    public float FadeAfter = 0f;

    [DataField]
    public float FadeTime = 0.3f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RappelDustComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField, AutoNetworkedField]
    public float Height;

    [DataField, AutoNetworkedField]
    public float Drop;

    [DataField, AutoNetworkedField]
    public Vector2 Drift;

    [DataField]
    public float Duration = 2.2f;

    [DataField]
    public float StartScale = 0.6f;

    [DataField]
    public float EndScale = 1.8f;

    [DataField]
    public float MaxAlpha = 0.75f;
}
