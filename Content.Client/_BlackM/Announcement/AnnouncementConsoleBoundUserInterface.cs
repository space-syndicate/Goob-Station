using Content.Shared._BlackM.Announcement;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._BlackM.Announcement;

[UsedImplicitly]
public sealed class AnnouncementConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AnnouncementConsoleWindow? _window;

    public AnnouncementConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AnnouncementConsoleWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnSendPressed += (channel, text) =>
        {
            SendMessage(new AnnouncementConsoleSendMessage(channel, text));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AnnouncementConsoleBuiState cast)
            _window?.UpdateState(cast);
    }
}
