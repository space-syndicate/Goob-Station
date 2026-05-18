using Content.Shared.Alert;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormBloodSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WormBloodComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<WormBloodComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WormBloodComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<WormBloodComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        SyncAlert(ent.Owner, ent.Comp);
    }

    private void OnStartup(Entity<WormBloodComponent> ent, ref ComponentStartup args)
    {
        SyncAlert(ent.Owner, ent.Comp);
    }

    private void OnAfterState(Entity<WormBloodComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        SyncAlert(ent.Owner, ent.Comp);
    }

    private void SyncAlert(EntityUid uid, WormBloodComponent blood)
    {
        if (_player.LocalEntity != uid)
            return;

        if (!HasComp<AlertsComponent>(uid))
            return;

        var severity = SharedWormBloodSystem.GetSeverity(blood.Blood);
        _alerts.ShowAlert(uid, blood.BloodAlert, severity);
    }
}
