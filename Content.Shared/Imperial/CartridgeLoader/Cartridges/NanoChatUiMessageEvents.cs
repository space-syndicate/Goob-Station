using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum NanoChatUiAction { NotificationSwitch, SendLocation }

[Serializable, NetSerializable]
public sealed class NanoChatUiActionEvent(NanoChatUiAction action) : CartridgeMessageEvent
{
    public readonly NanoChatUiAction Action = action;
}

[Serializable, NetSerializable]
public sealed class NanoChatSendEvent(string text) : CartridgeMessageEvent
{
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class NanoChatSelectChatEvent(int chatId) : CartridgeMessageEvent
{
    public readonly int ChatId = chatId;
}

[Serializable, NetSerializable]
public sealed class NanoChatCreateChatEvent(string chatName) : CartridgeMessageEvent
{
    public readonly string ChatName = chatName;
}

[Serializable, NetSerializable]
public sealed class NanoChatAddMembersEvent(int chatId, List<NetEntity> addedMembers) : CartridgeMessageEvent
{
    public int ChatId = chatId;
    public List<NetEntity> AddedMembers = addedMembers;
}

[Serializable, NetSerializable]
public sealed class NanoChatRemoveMembersEvent(int chatId, List<NetEntity> removedMembers) : CartridgeMessageEvent
{
    public int ChatId = chatId;
    public List<NetEntity> RemovedMembers = removedMembers;
}

[Serializable, NetSerializable]
public sealed class NanoChatEditChatEvent(int chatId, string newName) : CartridgeMessageEvent
{
    public int ChatId = chatId;
    public string NewName = newName;
}

[Serializable, NetSerializable]
public sealed class NanoChatTypingEvent : CartridgeMessageEvent;

[Serializable, NetSerializable]
public sealed class NanoChatPrintEvent : CartridgeMessageEvent;

