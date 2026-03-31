using Content.Shared.Administration.Logs;
using Content.Shared.Imperial.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Imperial.Atmos.Piping.Binary.Systems;

public abstract class SharedHydrogenGasVolumePumpSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HydrogenGasVolumePumpComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HydrogenGasVolumePumpComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<HydrogenGasVolumePumpComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HydrogenGasVolumePumpComponent, HydrogenGasVolumePumpToggleStatusMessage>(OnToggleStatusMessage);
        SubscribeLocalEvent<HydrogenGasVolumePumpComponent, HydrogenGasVolumePumpChangeTransferRateMessage>(OnTransferRateChangeMessage);
    }

    private void OnInit(Entity<HydrogenGasVolumePumpComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void OnPowerChanged(Entity<HydrogenGasVolumePumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    protected virtual void UpdateUi(Entity<HydrogenGasVolumePumpComponent> entity)
    {

    }

    private void OnToggleStatusMessage(EntityUid uid, HydrogenGasVolumePumpComponent pump, HydrogenGasVolumePumpToggleStatusMessage args)
    {
        pump.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");

        Dirty(uid, pump);
        UpdateUi((uid, pump));
        UpdateAppearance(uid, pump);
    }

    private void OnTransferRateChangeMessage(EntityUid uid, HydrogenGasVolumePumpComponent pump, HydrogenGasVolumePumpChangeTransferRateMessage args)
    {
        pump.TransferRate = Math.Clamp(args.TransferRate, 0f, pump.MaxTransferRate);
        Dirty(uid, pump);
        UpdateUi((uid, pump));
        _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the transfer rate on {ToPrettyString(uid):device} to {args.TransferRate}");
    }

    private void OnExamined(EntityUid uid, HydrogenGasVolumePumpComponent pump, ExaminedEvent args)
    {
        if (!Transform(uid).Anchored)
            return;

        if (Loc.TryGetString("gas-volume-pump-system-examined",
                out var str,
                ("statusColor", "lightblue"), // TODO: change with volume?
                ("rate", pump.TransferRate)
            ))
        {
            args.PushMarkup(str);
        }
    }

    protected void UpdateAppearance(EntityUid uid, HydrogenGasVolumePumpComponent? pump = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref pump, ref appearance, false))
            return;

        bool pumpOn = pump.Enabled && _receiver.IsPowered(uid);
        if (!pumpOn)
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.Off, appearance);
        else if (pump.Blocked)
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.Blocked, appearance);
        else
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.On, appearance);
    }
}
