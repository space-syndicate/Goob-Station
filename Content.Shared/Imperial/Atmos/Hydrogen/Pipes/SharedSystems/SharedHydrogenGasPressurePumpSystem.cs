using Content.Shared.Administration.Logs;
using Content.Shared.Imperial.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Imperial.Atmos.Piping.Binary.Components;

namespace Content.Shared.Imperial.Atmos.EntitySystems;

public abstract class SharedHydrogenGasPressurePumpSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    // TODO: Check enabled for activatableUI
    // TODO: Add activatableUI to it.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HydrogenGasPressurePumpComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HydrogenGasPressurePumpComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<HydrogenGasPressurePumpComponent, HydrogenGasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<HydrogenGasPressurePumpComponent, HydrogenGasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);

        SubscribeLocalEvent<HydrogenGasPressurePumpComponent, AtmosDeviceDisabledEvent>(OnPumpLeaveAtmosphere);
        SubscribeLocalEvent<HydrogenGasPressurePumpComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<HydrogenGasPressurePumpComponent> ent, ref ExaminedEvent args)
    {
        if (!Transform(ent).Anchored)
            return;

        if (Loc.TryGetString("gas-pressure-pump-system-examined",
                out var str,
                ("statusColor", "lightblue"), // TODO: change with pressure?
                ("pressure", ent.Comp.TargetPressure)
            ))
        {
            args.PushMarkup(str);
        }
    }

    private void OnInit(Entity<HydrogenGasPressurePumpComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnPowerChanged(Entity<HydrogenGasPressurePumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<HydrogenGasPressurePumpComponent, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        var pumpOn = ent.Comp1.Enabled && _receiver.IsPowered(ent.Owner);
        _appearance.SetData(ent, PumpVisuals.Enabled, pumpOn, ent.Comp2);
    }

    private void OnToggleStatusMessage(Entity<HydrogenGasPressurePumpComponent> ent, ref HydrogenGasPressurePumpToggleStatusMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent):device} to {args.Enabled}");
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnOutputPressureChangeMessage(Entity<HydrogenGasPressurePumpComponent> ent, ref HydrogenGasPressurePumpChangeOutputPressureMessage args)
    {
        ent.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.HydrogenMaxOutputPressure);
        _adminLogger.Add(LogType.AtmosPressureChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(ent):device} to {args.Pressure}kPa");
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnPumpLeaveAtmosphere(Entity<HydrogenGasPressurePumpComponent> ent, ref AtmosDeviceDisabledEvent args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);
        UpdateAppearance(ent);

        UserInterfaceSystem.CloseUi(ent.Owner, HydrogenGasPressurePumpUiKey.Key);
    }

    protected virtual void UpdateUi(Entity<HydrogenGasPressurePumpComponent> ent)
    {
    }
}
