using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.FaxTracker;

[Serializable, NetSerializable]
public sealed class FaxTrackerUiMessageEvent : CartridgeMessageEvent
{
    public readonly FaxTrackerUiAction Action;
    public readonly int Number;
    public readonly string? Address;

    public FaxTrackerUiMessageEvent(FaxTrackerUiAction action, int number = 0, string? address = null)
    {
        Action = action;
        Number = number;
        Address = address;
    }
}

[Serializable, NetSerializable]
public enum FaxTrackerUiAction : byte
{
    ClearHistory,
    ToggleNotifications,
    RemoveEntry,
    ToggleFavorite,
    BlacklistAdd,
    BlacklistRemove,
}
