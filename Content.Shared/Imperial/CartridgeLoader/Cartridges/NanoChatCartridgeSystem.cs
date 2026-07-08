using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent);
    }

    private void OnUiMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        switch (args)
        {
            case NanoChatSelectContactMessage select:
                ent.Comp.SelectedContact = select.ContactName;
                break;

            case NanoChatSendTextMessage send:
            {
                if (string.IsNullOrWhiteSpace(send.Text) || string.IsNullOrEmpty(ent.Comp.SelectedContact))
                    break;

                if (!ent.Comp.ChatHistories.TryGetValue(ent.Comp.SelectedContact, out var history))
                {
                    history = new List<NanoChatMessage>();
                    ent.Comp.ChatHistories[ent.Comp.SelectedContact] = history;
                }

                history.Add(new NanoChatMessage(ent.Comp.PdaCardName, send.Text));

                if (ent.Comp.SelectedContact == "Station AI")
                    history.Add(new NanoChatMessage("Station AI", "Request acknowledged. Processing..."));

                break;
            }

            case NanoChatUiActionMessage action:
                if (action.Action == NanoChatUiAction.NotificationSwitch)
                    ent.Comp.NotificationsOn = !ent.Comp.NotificationsOn;
                break;

            default:
                return;
        }

        UpdateUi(ent);
    }

    private void UpdateUi(Entity<NanoChatCartridgeComponent> ent)
    {
        var comp = ent.Comp;
        var loaderUid = Transform(ent).ParentUid;
        Log.Debug($"{loaderUid}");

        if (!loaderUid.IsValid())
            return;

        if (TryComp<PdaComponent>(loaderUid, out var pdaComp))
        {
            Log.Debug($"Имя владельца КПК: {pdaComp.OwnerName}");
            comp.PdaCardName = pdaComp.OwnerName;
        }

        Log.Debug($"Parent = {loaderUid}");
        Log.Debug($"Has PDA = {HasComp<PdaComponent>(loaderUid)}");
        Log.Debug($"Has Loader = {HasComp<CartridgeLoaderComponent>(loaderUid)}");

        var history = comp.SelectedContact != null && comp.ChatHistories.TryGetValue(comp.SelectedContact, out var chatHistory)
            ? chatHistory
            : new List<NanoChatMessage>();

        Log.Debug($"Sending PDA name '{comp.PdaCardName}'");

        var state = new NanoChatBoundUserInterfaceState(
            comp.NotificationsOn,
            comp.SelectedContact,
            comp.PdaCardName,
            comp.Contacts,
            history
        );

        CartridgeLoaderComponent? loader = null!;

        if (!Resolve(loaderUid, ref loader))
            return;

        if (_userInterfaceSystem.HasUi(loaderUid, loader.UiKey))
            _userInterfaceSystem.SetUiState(loaderUid, loader.UiKey, state);
    }
}
