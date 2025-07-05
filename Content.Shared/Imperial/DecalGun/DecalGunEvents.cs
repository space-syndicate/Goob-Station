using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.DecalGun;

[Serializable]
[NetSerializable]
public enum DecalGunKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DecalGunSelectionChangedMessage : BoundUserInterfaceMessage
{
    public string DecalId;
    public Color Color;
    public bool SnapToTile;
    public float Rotation;
    public bool IsCleanable;

    public DecalGunSelectionChangedMessage(string decalId, Color color, bool snap, float rotation, bool isCleanable)
    {
        DecalId = decalId;
        Color = color;
        SnapToTile = snap;
        Rotation = rotation;
        IsCleanable = isCleanable;
    }
}

[Serializable, NetSerializable]
public sealed class DecalGunUIState : BoundUserInterfaceState
{
    public readonly string DecalId;
    public readonly Color Color;
    public readonly bool Snap;
    public float Rotation;
    public bool IsCleanable;

    public DecalGunUIState(string decalId, Color color, bool snap, float rotation, bool isCleanable)
    {
        DecalId = decalId;
        Color = color;
        Snap = snap;
        Rotation = rotation;
        IsCleanable = isCleanable;
    }
}
