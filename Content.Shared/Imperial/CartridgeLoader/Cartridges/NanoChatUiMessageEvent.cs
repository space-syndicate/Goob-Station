using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum NanoChatUiAction
{
    NotificationSwitch,
}

[Serializable, NetSerializable]
public sealed class NanoChatUiActionMessage(NanoChatUiAction action) : CartridgeMessageEvent
{
    public readonly NanoChatUiAction Action = action;
}

[Serializable, NetSerializable]
public sealed class NanoChatSendTextMessage(string text) : CartridgeMessageEvent
{
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class NanoChatSelectContactMessage(string contactName) : CartridgeMessageEvent
{
    public readonly string ContactName = contactName;
}
