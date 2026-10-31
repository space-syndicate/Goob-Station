using Content.Server._CorvaxGoob.PowerToggle;
using Content.Shared._CorvaxGoob.OverTemperature;
using Content.Shared._CorvaxGoob.PowerToggle;
using Content.Shared.Examine;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.VendingMachines;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.OverTemperature;

public sealed partial class OverTemperatureSystem : EntitySystem
{
    [Dependency] private readonly TogglePowerSystem _togglePower = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OverTemperatureComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<OverTemperatureComponent, AttemptTogglePowerEvent>(OnTryTogglePower);
        SubscribeLocalEvent<OverTemperatureComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<OverTemperatureComponent, VendingMachineStateChangeEvent>(OnVendingMachineStateChange);

    }

    private void OnExamined(Entity<OverTemperatureComponent> entity, ref ExaminedEvent ev)
    {
        if (!ev.IsInDetailsRange)
            return;

        switch (entity.Comp.Status)
        {
            case OverTemperatureStatus.Okay:
                break;
            case OverTemperatureStatus.Over:
                if (TryComp<TemperatureDamageComponent>(entity, out var temperatureDamage) && temperatureDamage.Enabled)
                    ev.PushMarkup(Loc.GetString("overtemperature-examine-status-over"));
                break;
            case OverTemperatureStatus.Crit:
                if (entity.Comp.TemperatureThresholdEnabled)
                    ev.PushMarkup(Loc.GetString("overtemperature-examine-status-crit"));
                break;
        }

        
    }

    private void OnVendingMachineStateChange(Entity<OverTemperatureComponent> entity, ref VendingMachineStateChangeEvent ev)
    {
        if (!TryComp<TemperatureDamageComponent>(entity, out var temperatureDamage))
            return;

        temperatureDamage.Enabled = !ev.Broken;
    }

    private void OnTemperatureChange(Entity<OverTemperatureComponent> entity, ref OnTemperatureChangeEvent args)
    {
        var status = GetStatus(entity, args.CurrentTemperature);

        if (entity.Comp.TemperatureThresholdEnabled || TryComp<TemperatureDamageComponent>(entity, out var temperatureDamage) && temperatureDamage.Enabled)
            if (status != OverTemperatureStatus.Okay && _timing.CurTime > entity.Comp.NextAlertTime)
            {
                entity.Comp.NextAlertTime = _timing.CurTime + entity.Comp.IntervalAlertTime;

                if (Transform(entity).Anchored)
                    _audio.PlayPvs(status == OverTemperatureStatus.Crit ? entity.Comp.CritTemparatureAlertSound : entity.Comp.OverTemparatureAlertSound, entity);
            }

        if (entity.Comp.Status == status)
            return;

        entity.Comp.Status = status;

        if (HasComp<AppearanceComponent>(entity))
            _appearance.SetData(entity, OverTemperatureStatusInfo.Status, status);

        if (TryComp<TogglePowerComponent>(entity, out var toggler) && toggler.IsTurnedOn && status == OverTemperatureStatus.Crit)
            _togglePower.SetPower((entity.Owner, toggler), false);
    }

    private void OnTryTogglePower(Entity<OverTemperatureComponent> entity, ref AttemptTogglePowerEvent args)
    {
        if (entity.Comp.Status == OverTemperatureStatus.Crit)
            args.Cancelled = true;
    }

    private OverTemperatureStatus GetStatus(Entity<OverTemperatureComponent> entity, float curTemp)
    {
        if (TryComp<TemperatureDamageComponent>(entity, out var tempDamage) && tempDamage.Enabled && curTemp > tempDamage.HeatDamageThreshold)
            return OverTemperatureStatus.Over;
        else if (entity.Comp.TemperatureThresholdEnabled && (curTemp > entity.Comp.OperationalTemperatureThreshold.Y
            || curTemp < entity.Comp.OperationalTemperatureThreshold.X))
            return OverTemperatureStatus.Crit;

        return OverTemperatureStatus.Okay;
    }
}

