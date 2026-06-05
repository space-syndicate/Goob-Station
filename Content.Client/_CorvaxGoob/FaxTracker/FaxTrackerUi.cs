using Content.Client.UserInterface.Fragments;
using Content.Shared._CorvaxGoob.FaxTracker;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._CorvaxGoob.FaxTracker;

public sealed partial class FaxTrackerUi : UIFragment
{
    private FaxTrackerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new FaxTrackerUiFragment();

        _fragment.OnClearPressed += () => SendMessage(new FaxTrackerUiMessageEvent(FaxTrackerUiAction.ClearHistory), userInterface);
        _fragment.OnNotificationPressed += () => SendMessage(new FaxTrackerUiMessageEvent(FaxTrackerUiAction.ToggleNotifications), userInterface);
        _fragment.OnEntryRemovePressed += number => SendMessage(new FaxTrackerUiMessageEvent(FaxTrackerUiAction.RemoveEntry, number), userInterface);
        _fragment.OnEntryFavoritePressed += number => SendMessage(new FaxTrackerUiMessageEvent(FaxTrackerUiAction.ToggleFavorite, number), userInterface);
        _fragment.OnBlacklistAdd += address => SendMessage(new FaxTrackerUiMessageEvent(FaxTrackerUiAction.BlacklistAdd, address: address), userInterface);
        _fragment.OnBlacklistRemove += address => SendMessage(new FaxTrackerUiMessageEvent(FaxTrackerUiAction.BlacklistRemove, address: address), userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is FaxTrackerUiState cast)
            _fragment?.UpdateState(cast);
    }

    private static void SendMessage(FaxTrackerUiMessageEvent message, BoundUserInterface userInterface)
    {
        userInterface.SendMessage(new CartridgeUiMessage(message));
    }
}
