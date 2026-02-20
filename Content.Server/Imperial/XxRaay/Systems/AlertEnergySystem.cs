using System;
using Content.Shared.Alert;
using Content.Shared.Imperial.XxRaay.Components;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class AlertEnergySystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private EntityQuery<AlertEnergyComponent> _energyQuery;

    public override void Initialize()
    {
        _energyQuery = GetEntityQuery<AlertEnergyComponent>();

        SubscribeLocalEvent<AlertEnergyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AlertEnergyComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, AlertEnergyComponent component, ref ComponentStartup args)
    {
        UpdateEnergyAlert(uid, component);
    }

    private void OnShutdown(EntityUid uid, AlertEnergyComponent component, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, component.AlertId);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<AlertEnergyComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (comp.RegenPerSecond <= 0f || comp.Energy >= comp.MaxEnergy)
                continue;

            var delta = comp.RegenPerSecond * frameTime;
            SetEnergy(uid, comp.Energy + delta, comp);
        }
    }

    public void SetEnergy(EntityUid uid, float value, AlertEnergyComponent? component = null)
    {
        if (!_energyQuery.Resolve(uid, ref component))
            return;

        var clamped = Math.Clamp(value, 0f, component.MaxEnergy);
        if (component.Energy == clamped)
            return;

        component.Energy = clamped;
        Dirty(uid, component);
        UpdateEnergyAlert(uid, component);
    }

    public void ModifyEnergy(EntityUid uid, float delta, AlertEnergyComponent? component = null)
    {
        if (!_energyQuery.Resolve(uid, ref component))
            return;

        SetEnergy(uid, component.Energy + delta, component);
    }

    private void UpdateEnergyAlert(EntityUid uid, AlertEnergyComponent component)
    {
        var step = component.Step <= 0 ? 1f : component.Step;
        var maxSeverity = (short) Math.Max(0, (int) Math.Floor(component.MaxEnergy / step));
        var severity = (short) Math.Clamp((int) Math.Floor(component.Energy / step), 0, maxSeverity);

        _alerts.UpdateAlert(uid, component.AlertId, severity);
    }
}

