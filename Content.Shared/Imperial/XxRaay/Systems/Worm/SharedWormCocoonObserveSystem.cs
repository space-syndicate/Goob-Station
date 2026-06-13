using Content.Shared.Actions;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedWormCocoonObserveSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormCocoonObserverComponent, WormCocoonObserveActionEvent>(OnObserveAction);
    }

    private void OnObserveAction(Entity<WormCocoonObserverComponent> ent, ref WormCocoonObserveActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_net.IsServer)
            return;

        ObserveRandomWorm(ent, args.Performer);
    }

    protected abstract void ObserveRandomWorm(Entity<WormCocoonObserverComponent> cocoon, EntityUid? performer);

    protected void ShowObservePopup(EntityUid cocoon, EntityUid? performer, string message)
    {
        if (performer != null)
            _popup.PopupEntity(message, cocoon, performer.Value, PopupType.Medium);
        else
            _popup.PopupEntity(message, cocoon, PopupType.Medium);
    }
}
