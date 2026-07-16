using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.Imperial.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.Imperial.CartridgeLoader.Cartridges;

public sealed partial class NanoChatUi : UIFragment
{
    private NanoChatUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NanoChatUiFragment();

        _fragment.OnNotificationSwitchPressed += () =>
        {
            SendChatMessage(new NanoChatUiActionEvent(NanoChatUiAction.NotificationSwitch), userInterface);
        };

        _fragment.OnSendLocation += () =>
        {
            SendChatMessage(new NanoChatUiActionEvent(NanoChatUiAction.SendLocation), userInterface);
        };

        _fragment.OnSendMessage += (text) =>
        {
            SendChatMessage(new NanoChatSendEvent(text), userInterface);
        };

        _fragment.OnChatSelected += (chatId) =>
        {
            SendChatMessage(new NanoChatSelectChatEvent(chatId), userInterface);
        };

        _fragment.OnCreateChat += (chatName) =>
        {
            SendChatMessage(new NanoChatCreateChatEvent(chatName), userInterface);
        };

        _fragment.OnTypingMessage += () =>
        {
            SendChatMessage(new NanoChatTypingEvent(), userInterface);
        };

        _fragment.OnAddMembers += (chatId, members) =>
        {
            SendChatMessage(new NanoChatAddMembersEvent(chatId, members), userInterface);
        };

        _fragment.OnRemoveMembers += (chatId, members) =>
        {
            SendChatMessage(new NanoChatRemoveMembersEvent(chatId, members), userInterface);
        };

        _fragment.OnEditChat += (chatId, newName) =>
        {
            SendChatMessage(new NanoChatEditChatEvent(chatId, newName), userInterface);
        };

        _fragment.OnPrint += () =>
        {
            SendChatMessage(new NanoChatPrintEvent(), userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NanoChatBoundUserInterfaceState chatState)
            _fragment?.UpdateState(chatState);
    }

    private static void SendChatMessage(CartridgeMessageEvent messageEvent, BoundUserInterface userInterface)
    {
        var message = new CartridgeUiMessage(messageEvent);
        userInterface.SendMessage(message);
    }
}
