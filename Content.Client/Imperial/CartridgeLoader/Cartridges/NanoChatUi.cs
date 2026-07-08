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
            SendChatMessage(new NanoChatUiActionMessage(NanoChatUiAction.NotificationSwitch), userInterface);
        };

        _fragment.OnSendMessage += (text) =>
        {
            SendChatMessage(new NanoChatSendTextMessage(text), userInterface);
        };

        _fragment.OnContactSelected += (contactName) =>
        {
            SendChatMessage(new NanoChatSelectContactMessage(contactName), userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NanoChatBoundUserInterfaceState chatState)
        {
            _fragment?.UpdateState(chatState);
        }
    }

    private void SendChatMessage(CartridgeMessageEvent messageEvent, BoundUserInterface userInterface)
    {
        var message = new CartridgeUiMessage(messageEvent);
        userInterface.SendMessage(message);
    }
}
