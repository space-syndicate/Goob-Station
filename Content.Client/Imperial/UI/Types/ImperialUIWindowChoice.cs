namespace Content.Client.Imperial.UI;


public sealed record ImperialUIWindowChoice(
    LocId ButtonText,
    LocId ButtonHeader,
    Action<object?> Callback,
    object? CallbackData = null
);
