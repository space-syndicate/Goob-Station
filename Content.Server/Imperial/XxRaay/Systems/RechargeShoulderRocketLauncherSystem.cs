using Content.Shared.Imperial.XxRaay.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// Система для автоперезарядки плечевой ракетной установки.
/// </summary>
public sealed class RechargeShoulderRocketLauncherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeShoulderRocketLauncherComponent, MapInitEvent>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<RechargeShoulderRocketLauncherComponent, ShoulderRocketLauncherComponent>();

        while (query.MoveNext(out var uid, out var recharge, out var launcher))
        {
            if (launcher.Charges >= launcher.MaxCharges || recharge.NextCharge == null)
                continue;

            if (recharge.NextCharge > _timing.CurTime)
                continue;

            launcher.Charges++;
            Dirty(uid, launcher);

            if (_netManager.IsServer)
                _audio.PlayPvs(recharge.RechargeSound, uid);

            if (launcher.Charges >= launcher.MaxCharges)
            {
                recharge.NextCharge = null;
                Dirty(uid, recharge);
                continue;
            }

            recharge.NextCharge = recharge.NextCharge.Value + TimeSpan.FromSeconds(recharge.RechargeCooldown);
            Dirty(uid, recharge);
        }
    }

    private void OnInit(EntityUid uid, RechargeShoulderRocketLauncherComponent component, MapInitEvent args)
    {
        component.NextCharge = _timing.CurTime;
        Dirty(uid, component);
    }

    public void Reset(EntityUid uid, RechargeShoulderRocketLauncherComponent? recharge = null)
    {
        if (!Resolve(uid, ref recharge, false))
            return;

        if (recharge.NextCharge == null || recharge.NextCharge < _timing.CurTime)
        {
            recharge.NextCharge = _timing.CurTime + TimeSpan.FromSeconds(recharge.RechargeCooldown);
            Dirty(uid, recharge);
        }
    }
}

