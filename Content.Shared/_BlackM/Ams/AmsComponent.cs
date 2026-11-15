using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._BlackM.Ams;

[NetSerializable, Serializable]
public enum AmsStatus : byte
{
    Inactive,

    Synchronizing,

    Ready,

    Active,

    Detonating,

    Destroyed,
}

public enum AmsPartType : byte
{
    Core,
    Coil,
    FocusingLens,
    PowerCell,
    Stabilizer,
    Casing,
}

[DataDefinition]
public sealed partial class AmsReinforcement
{
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField]
    public int Amount = 5;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AmsComponent : Component
{
    [DataField, AutoNetworkedField]
    public AmsStatus Status = AmsStatus.Inactive;

    [DataField, AutoNetworkedField]
    public TimeSpan? SyncStartTime;

    [DataField, AutoNetworkedField]
    public TimeSpan? ActiveEndTime;

    [DataField, AutoNetworkedField]
    public TimeSpan? DetonationTime;

    [DataField, AutoNetworkedField]
    public float ChargeTime = 900f;

    [DataField]
    public float ActiveDuration = 300f;

    [DataField, AutoNetworkedField]
    public float DetonationDelay = 60f;

    [DataField]
    public float HecuSpawnDelay = 20f;

    [DataField]
    public List<string> PartSlots = new()
    {
        "ams_part_core",
        "ams_part_coil",
        "ams_part_lens",
        "ams_part_cell",
        "ams_part_stabilizer",
        "ams_part_casing",
    };

    [DataField]
    public Dictionary<string, AmsReinforcement> Reinforcements = new();

    [DataField]
    public float ReinforceDelay = 2f;

    [DataField]
    public bool RequireSample = true;

    [DataField, AutoNetworkedField]
    public Vector2 SampleOffset = new(0f, -1f);

    [DataField, AutoNetworkedField]
    public float SampleRange = 0.6f;

    [DataField, AutoNetworkedField]
    public bool Primed;

    [ViewVariables] public readonly Dictionary<string, int> Reinforced = new();
    [ViewVariables] public readonly HashSet<string> Levers = new();
    [ViewVariables] public EntityUid? Sample;
    [ViewVariables] public TimeSpan NextSampleCheck;

    [DataField]
    public int SpecialDoorRequiredParts = 4;

    [DataField]
    public float ZenReductionPerPart = 15f;

    [DataField]
    public List<string> InfectionDecals = new() { "AmsInfectedDecal1", "AmsInfectedDecal2" };

    [DataField]
    public float InfectionStartRate = 1.5f;

    [DataField]
    public float InfectionEndRate = 10f;

    [DataField] public string ExplosionType = "Default";

    [DataField] public float CoreIntensity = 60000f;
    [DataField] public float CoreSlope = 5f;
    [DataField] public float CoreMaxTileIntensity = 200f;

    [DataField] public float WaveSpeed = 14f;

    [DataField] public int WaveExplosionSpacing = 7;

    [DataField] public float WaveExplosionChance = 0.45f;

    [DataField] public float WaveExplosionsPerSecond = 14f;

    [DataField] public float WaveIntensity = 600f;
    [DataField] public float WaveSlope = 3f;
    [DataField] public float WaveMaxTileIntensity = 30f;

    [DataField] public float WaveScorchChance = 0.12f;

    [DataField] public EntProtoId PortalPrototype = "AmsXenPortal";

    [DataField] public float PortalIntervalStart = 4f;
    [DataField] public float PortalIntervalEnd = 0.6f;

    [DataField] public List<EntProtoId> PortalSpawns = new();
    [DataField] public float PortalSpawnChance = 0.25f;

    [DataField]
    public float RoundEndDelay = 30f;

    [ViewVariables] public bool HecuSpawned;

    [ViewVariables] public bool Launched;
    [ViewVariables] public bool ClosingWarningSent;
    [ViewVariables] public bool SpecialDoorOpened;
    [ViewVariables] public TimeSpan NextUiUpdate;
    [ViewVariables] public TimeSpan NextFlicker;
    [ViewVariables] public TimeSpan NextPortal;
}

[RegisterComponent]
public sealed partial class AmsPartComponent : Component
{
    [DataField(required: true)]
    public AmsPartType PartType;
}

[RegisterComponent]
public sealed partial class AmsSampleComponent : Component
{
    [ViewVariables] public bool Used;
}

[Serializable, NetSerializable]
public enum AmsSampleVisuals : byte
{
    Active,
}

[RegisterComponent]
public sealed partial class HecuSpawnPointComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "RandomHumanoidHECUSoldierSpawner";
}

[RegisterComponent]
public sealed partial class AmsControlledDoorComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string GroupId = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string AnnouncementLocationName = string.Empty;
}

public static class AmsHelpers
{
    public static float GetCharge(AmsStatus status, TimeSpan? syncStart, float chargeTime, TimeSpan now)
    {
        return status switch
        {
            AmsStatus.Synchronizing when syncStart is { } start && chargeTime > 0f
                => Math.Clamp((float) (now - start).TotalSeconds / chargeTime, 0f, 1f),
            AmsStatus.Ready or AmsStatus.Active or AmsStatus.Detonating or AmsStatus.Destroyed => 1f,
            _ => 0f,
        };
    }

    public static float GetDetonationProgress(AmsComponent ams, TimeSpan now)
    {
        if (ams.Status != AmsStatus.Detonating || ams.DetonationTime is not { } t)
            return ams.Status == AmsStatus.Destroyed ? 1f : 0f;

        var left = (float) (t - now).TotalSeconds;
        return 1f - Math.Clamp(left / MathF.Max(ams.DetonationDelay, 1f), 0f, 1f);
    }
}
