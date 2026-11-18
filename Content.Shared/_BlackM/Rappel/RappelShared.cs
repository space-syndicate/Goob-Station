using System.Numerics;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._BlackM.Rappel;

[Serializable, NetSerializable]
public enum RappelConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RappelConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public Vector2[] AvailableSlots = Array.Empty<Vector2>();
    public Vector2[] BlockedSlots = Array.Empty<Vector2>();
    public Vector2[] ReservedSlots = Array.Empty<Vector2>();
    public NetEntity? Camera;
    public string ZoneName;
    public Vector2 ZoneCenter;
    public float ZoneRadius;
    public Vector2? Target;
    public float SlotSpacing;
    public float SlotAngleOffset;
    public int RopeCount;
    public float CameraZoom;
    public float MaxZoom;
    public bool ZoneFound;

    public RappelConsoleBoundUserInterfaceState(
        NetEntity? camera,
        string zoneName,
        Vector2 zoneCenter,
        float zoneRadius,
        Vector2? target,
        float slotSpacing,
        float slotAngleOffset,
        int ropeCount,
        float cameraZoom,
        float maxZoom,
        bool zoneFound)
    {
        Camera = camera;
        ZoneName = zoneName;
        ZoneCenter = zoneCenter;
        ZoneRadius = zoneRadius;
        Target = target;
        SlotSpacing = slotSpacing;
        SlotAngleOffset = slotAngleOffset;
        RopeCount = ropeCount;
        CameraZoom = cameraZoom;
        MaxZoom = maxZoom;
        ZoneFound = zoneFound;
    }
}

[Serializable, NetSerializable]
public sealed class RappelConsoleSelectPointMessage : BoundUserInterfaceMessage
{
    public Vector2 Position;

    public RappelConsoleSelectPointMessage(Vector2 position)
    {
        Position = position;
    }
}

[Serializable, NetSerializable]
public sealed class RappelConsoleClearPointMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RappelConsoleMoveCameraMessage : BoundUserInterfaceMessage
{
    public Vector2 Position;

    public RappelConsoleMoveCameraMessage(Vector2 position)
    {
        Position = position;
    }
}

[Serializable, NetSerializable]
public sealed class RappelConsoleRecenterMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed partial class RappelDoAfterEvent : SimpleDoAfterEvent;

public static class RappelSlots
{
    public const float GoldenAngle = 2.39996323f;

    public const int MaxCandidates = 96;

    public static Vector2 GetSpiralOffset(int index, float spacing, float angleOffset)
    {
        if (index <= 0)
            return Vector2.Zero;

        var radius = spacing * 1.05f * MathF.Sqrt(index);
        var angle = index * GoldenAngle + angleOffset;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }
}

[Serializable, NetSerializable]
public enum RappelRopeVisuals : byte
{
    Linked,
}

[Serializable, NetSerializable]
public enum RappelRopeVisualLayers : byte
{
    Rope,
    Light,
}

[Serializable, NetSerializable]
public enum RappelDecorVisuals : byte
{
    Landed,
}
