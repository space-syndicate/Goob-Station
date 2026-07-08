namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NanoChatCartridgeComponent : Component
{
    public string? PdaCardName;

    public List<string> Contacts = new()
    {
        "Captain",
        "Chief Engineer",
        "Station AI",
        "Honkbot",
    };

    public Dictionary<string, List<NanoChatMessage>> ChatHistories = new()
    {
        {
            "Captain",
            new List<NanoChatMessage>
            {
                new("Captain", "Get to the bridge immediately!"),
                new("You", "On my way."),
            }
        },
        {
            "Station AI",
            new List<NanoChatMessage>
            {
                new("Station AI", "Atmospherics status: Nominal."),
            }
        },
    };

    public string? SelectedContact;
    public bool NotificationsOn = true;
}
