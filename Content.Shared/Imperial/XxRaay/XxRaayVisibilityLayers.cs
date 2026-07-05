namespace Content.Shared.Imperial.XxRaay;

public static class XxRaayVisibilityLayers
{
    public const ushort Normal = 1;
    public const ushort SpiderVent = 1 << 4;
    public const ushort DoorHide = 1 << 5;
    public const int SpiderVentMask = SpiderVent;
    public const int DoorHideMask = DoorHide;
}
