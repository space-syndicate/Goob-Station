using Content.Server.Atmos.EntitySystems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Imperial.Power.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Imperial.Power;
using Content.Shared.Imperial.Power.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Imperial.Power.Components.EventComponents;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.DeviceLinking;
using Content.Shared.Imperial.Power.Events;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterConsoleSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosSystem = null!;
    [Dependency] private readonly ChatSystem _chatSystem = null!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly RadioSystem _radioSystem = null!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = null!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly IComponentFactory _componentFactory = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterConsoleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<SupermatterConsoleComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<SupermatterConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<SupermatterConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<SupermatterConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);

        SubscribeLocalEvent<SupermatterIntegrityComponent, SupermatterStartupEvent>(OnSupermatterStartup);
        SubscribeLocalEvent<SupermatterIntegrityComponent, SupermatterSendRadioEvent>(OnSendRadioEvent);
    }

    private void OnInit(Entity<SupermatterConsoleComponent> entity, ref ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(entity, entity.Comp.InputPort);
    }

    private void OnMapInit(Entity<SupermatterConsoleComponent> entity, ref MapInitEvent args)
    {
        if (TryComp<DeviceLinkSinkComponent>(entity, out var sink))
        {
            foreach (var sourceUid in sink.LinkedSources.Where(HasComp<SupermatterIntegrityComponent>))
            {
                entity.Comp.ConnectedSupermatter = sourceUid;
                _signalSystem.LinkDefaults(null, sourceUid, entity.Owner);
                return;
            }
        }

        entity.Comp.ConnectedSupermatter ??= FindNearestSupermatter(entity);
    }

    private void OnLinkAttempt(Entity<SupermatterConsoleComponent> entity, ref LinkAttemptEvent args)
    {
        if (args.SinkPort != entity.Comp.InputPort || HasComp<SupermatterIntegrityComponent>(args.Source))
            return;

        args.Cancel();
    }

    private static void OnNewLink(Entity<SupermatterConsoleComponent> entity, ref NewLinkEvent args)
    {
        entity.Comp.ConnectedSupermatter = args.Source;
        entity.Comp.NextUiUpdate = TimeSpan.Zero;
    }

    private void OnPortDisconnected(Entity<SupermatterConsoleComponent> entity, ref PortDisconnectedEvent args)
    {
        if (args.Port != entity.Comp.InputPort)
            return;
        entity.Comp.ConnectedSupermatter = null;
        UpdateUi(entity, true);
    }

    private static void OnUiOpened(Entity<SupermatterConsoleComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(SupermatterConsoleUiKey.Key))
            return;

        entity.Comp.NextUiUpdate = TimeSpan.Zero;
    }

    private void OnSupermatterStartup(Entity<SupermatterIntegrityComponent> supermatter, ref SupermatterStartupEvent args)
    {
        var query = EntityQueryEnumerator<SupermatterConsoleComponent, TransformComponent>();
        var smTrans = Transform(supermatter.Owner);

        while (query.MoveNext(out var consoleUid, out var consoleComp, out var consoleTrans))
        {
            if (consoleComp.ConnectedSupermatter != null)
                continue;
            var maxRange = consoleComp.MaxRange;

            if (!smTrans.Coordinates.TryDistance(EntityManager, consoleTrans.Coordinates, out var distance)
                || distance > maxRange)
                continue;

            _signalSystem.LinkDefaults(null, supermatter.Owner, consoleUid);
            consoleComp.ConnectedSupermatter = supermatter.Owner;
        }
    }

    private void OnSendRadioEvent(Entity<SupermatterIntegrityComponent> entity, ref SupermatterSendRadioEvent args)
    {
        var query = EntityQueryEnumerator<SupermatterConsoleComponent>();
        var radioSent = false;

        while (query.MoveNext(out var consoleUid, out var consoleComp))
        {
            if (consoleComp.ConnectedSupermatter != entity)
                continue;

            _chatSystem.TrySendInGameICMessage(consoleUid, args.Message, InGameICChatType.Speak, ChatTransmitRange.Normal);

            if (radioSent)
                continue;

            foreach (var channel in entity.Comp.RadioChannels)
                _radioSystem.SendRadioMessage(consoleUid, args.Message, channel, consoleUid);

            radioSent = true;
        }
    }

    private EntityUid? FindNearestSupermatter(Entity<SupermatterConsoleComponent> console)
    {
        var pos = Transform(console).Coordinates;
        EntityUid? nearest = null;
        var minDist = console.Comp.MaxRange;

        var smEnumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
        while (smEnumerator.MoveNext(out var smUid, out _, out var smTrans))
        {
            if (!pos.TryDistance(EntityManager, smTrans.Coordinates, out var dist)
                || dist > minDist)
                continue;

            minDist = dist;
            nearest = smUid;
        }

        if (nearest != null)
            _signalSystem.LinkDefaults(null, nearest.Value, console);

        return nearest;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SupermatterConsoleComponent>();
        while (enumerator.MoveNext(out var consUid, out var console))
        {
            var uiOpen = _uiSystem.IsUiOpen(consUid, SupermatterConsoleUiKey.Key);

            if (console.ConnectedSupermatter == null)
            {
                if (uiOpen)
                    UpdateUi((consUid, console));
                continue;
            }

            var smUid = console.ConnectedSupermatter.Value;
            if (!TryComp<SupermatterIntegrityComponent>(smUid, out var integrityComponent))
            {
                console.ConnectedSupermatter = null;
                if (Exists(smUid))
                    _signalSystem.RemoveSinkFromSource(smUid, consUid);

                if (uiOpen)
                    UpdateUi((consUid, console));
                continue;
            }

            var (integrityPercent, level) = CalculateIntegrity(integrityComponent);
            var integrity = (int)Math.Round(integrityPercent);
            var highestLevel = integrityComponent.SupermatterIntegrity.MaxBy(e => e.Threshold);

            if (highestLevel == null || integrity >= highestLevel.Threshold)
                console.NextBeep = _timing.CurTime + console.BeepInterval;
            else if (integrityComponent.Activated && _timing.CurTime >= console.NextBeep)
            {
                _audioSystem.PlayPvs(console.BeepSound, consUid);
                console.NextBeep = _timing.CurTime + console.BeepInterval;
            }

            if (!uiOpen || _timing.CurTime < console.NextUiUpdate)
                continue;

            console.NextUiUpdate = _timing.CurTime + console.UiUpdateInterval;

            var supermatterEv = "—";
            if (TryComp<SupermatterEventSchedulerComponent>(smUid, out var scheduler) && scheduler.Events?.Components is not null)
            {
                var locList = new List<string>();
                foreach (var compStr in scheduler.Events.Components)
                {
                    if (!_componentFactory.TryGetRegistration(compStr, out var registration))
                        continue;

                    if (!EntityManager.TryGetComponent(smUid, registration.Type, out var comp) ||
                        comp is not ISupermatterEventComponent eventComp)
                        continue;

                    if (eventComp.EventName != null)
                        locList.Add(Loc.GetString(eventComp.EventName));
                }

                supermatterEv = locList.Count > 0
                    ? string.Join(", ", locList)
                    : "—";
            }

            var transComp = Transform(smUid);
            var gas = _atmosSystem.GetContainingMixture((smUid, transComp));

            var pressure = gas?.Pressure ?? 0f;
            var temperature = gas?.Temperature ?? 0f;

            var state = new SupermatterConsoleBuiState(
                activated: integrityComponent.Activated,
                temperature: temperature,
                integrityComponent.TempThresholds,
                pressure: pressure,
                integrityComponent.PressureThresholds,
                integrity: integrityPercent,
                integrityColor: level.Color,
                currentEvent: supermatterEv
            );

            _uiSystem.SetUiState(consUid, SupermatterConsoleUiKey.Key, state);
        }
    }

    private void UpdateUi(Entity<SupermatterConsoleComponent> entity, bool force = false)
    {
        if (!_uiSystem.IsUiOpen(entity.Owner, SupermatterConsoleUiKey.Key))
            return;
        if (!force && _timing.CurTime < entity.Comp.NextUiUpdate)
            return;

        entity.Comp.NextUiUpdate = _timing.CurTime + entity.Comp.UiUpdateInterval;

        _uiSystem.SetUiState(
            entity.Owner,
            SupermatterConsoleUiKey.Key,
            new SupermatterConsoleBuiState(activated: false));
    }

    private static (float integrity, SupermatterIntegrityLevel integrityLevel)
        CalculateIntegrity(SupermatterIntegrityComponent component)
    {
        var integrity = component.Integrity / component.MaxIntegrity * 100f;

        foreach (var level in component.SupermatterIntegrity.Where(level => integrity >= level.Threshold))
            return (integrity, level);

        return (integrity, component.SupermatterIntegrity[^1]);
    }
}
