using Content.Shared.Alert;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Helpers;
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
        SubscribeLocalEvent<WormBloodComponent, AfterAutoHandleStateEvent>(OnSyncAlert);
        SubscribeLocalEvent<WormBloodComponent, ComponentStartup>(OnSyncAlert);
        SubscribeLocalEvent<WormBloodComponent, LocalPlayerAttachedEvent>(OnSyncAlert);
    }

    private void OnSyncAlert<T>(Entity<WormBloodComponent> ent, ref T args)
    {
        SyncAlert(ent.Owner, ent.Comp);
    }

    private void SyncAlert(EntityUid uid, WormBloodComponent blood)
    {
        if (_player.LocalEntity != uid)
            return;

        if (!HasComp<AlertsComponent>(uid))
            return;

        var severity = WormBloodHelper.GetSeverity(blood.Blood);
        _alerts.ShowAlert(uid, blood.BloodAlert, severity);
    }
}
