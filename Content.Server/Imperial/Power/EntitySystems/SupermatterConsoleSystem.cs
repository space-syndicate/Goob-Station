using Content.Server.Atmos.EntitySystems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Imperial.Power.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Imperial.Power;
using Content.Shared.Imperial.Power.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using System.Linq;

namespace Content.Server.Imperial.Power.EntitySystems;

public sealed class SupermatterConsoleSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosSystem = null!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = null!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = null!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SupermatterConsoleComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<SupermatterConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<SupermatterConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
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

    private void OnNewLink(Entity<SupermatterConsoleComponent> entity, ref NewLinkEvent args)
    {
        entity.Comp.ConnectedSupermatter = args.Source;
    }

    private void OnPortDisconnected(Entity<SupermatterConsoleComponent> entity, ref PortDisconnectedEvent args)
    {
        if (args.Port != entity.Comp.InputPort)
            return;
        entity.Comp.ConnectedSupermatter = null;
        ResetConsoleUi(entity, 0f);
    }

    private EntityUid? FindNearestSupermatter(EntityUid consoleUid)
    {
        var transformCompConsole = Transform(consoleUid);
        var mapId = transformCompConsole.MapID;
        var pos = _transformSystem.GetMapCoordinates(transformCompConsole).Position;

        EntityUid? nearest = null;
        var minDist = float.MaxValue;

        var smEnumerator = EntityQueryEnumerator<SupermatterIntegrityComponent, TransformComponent>();
        while (smEnumerator.MoveNext(out var smUid, out _, out var transComp))
        {
            if (transComp.MapID != mapId)
                continue;

            var smPos = _transformSystem.GetMapCoordinates(smUid).Position;
            var dist = (smPos - pos).LengthSquared();
            if (dist > minDist)
                continue;

            minDist = dist;
            nearest = smUid;
        }

        if (nearest != null)
            _signalSystem.LinkDefaults(null, nearest.Value, consoleUid);

        return nearest;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SupermatterConsoleComponent>();
        while (enumerator.MoveNext(out var consUid, out var console))
        {
            if (console.ConnectedSupermatter == null)
            {
                ResetConsoleUi((consUid, console), frameTime);
                continue;
            }

            var smUid = console.ConnectedSupermatter.Value;

            if (!TryComp<SupermatterIntegrityComponent>(smUid, out var nearest))
            {
                console.ConnectedSupermatter = null;
                _signalSystem.RemoveSinkFromSource(smUid, consUid);
                ResetConsoleUi((consUid, console), frameTime);
                continue;
            }

            var supermatterEv = "—";
            if (TryComp<SupermatterEventComponent>(smUid, out var eventComponent))
                supermatterEv = Loc.GetString($"supermatter-event-{eventComponent.CurrentEvent.ToString().ToLowerInvariant()}-name");

            var (integrityPercent, level) = CalculateIntegrity(nearest);
            var integrity = (int)Math.Round(integrityPercent);

            console.UiUpdateTimer -= TimeSpan.FromSeconds(frameTime);
            if (console.UiUpdateTimer <= TimeSpan.Zero)
            {
                console.UiUpdateTimer = console.UiUpdateInterval;

                if (_uiSystem.IsUiOpen(consUid, SupermatterConsoleUiKey.Key))
                {
                    var transComp = Transform(smUid);
                    var gas = _atmosSystem.GetContainingMixture((smUid, transComp));

                    var pressure = gas?.Pressure ?? 0f;
                    var temperature = gas?.Temperature ?? 0f;

                    var state = new SupermatterConsoleBuiState(
                        activated: nearest.Activated,
                        temperature: temperature,
                        lowerTemperature: nearest.LowerTempThreshold,
                        upperTemperature: nearest.UpperTempThreshold,
                        pressure: pressure,
                        lowerPressure: nearest.LowerPressureThreshold,
                        upperPressure: nearest.UpperPressureThreshold,
                        integrity: integrityPercent,
                        integrityColor: level.Color,
                        currentEvent: supermatterEv
                    );

                    _uiSystem.SetUiState(consUid, SupermatterConsoleUiKey.Key, state);
                }
            }

            var highestLevel = nearest.SupermatterIntegrity.MaxBy(e => e.Threshold);

            if (highestLevel == null || integrity >= highestLevel.Threshold)
                console.BeepCooldownTimer = TimeSpan.Zero;
            else if (nearest.Activated)
            {
                console.BeepCooldownTimer -= TimeSpan.FromSeconds(frameTime);
                if (console.BeepCooldownTimer > TimeSpan.Zero)
                    continue;
                _audioSystem.PlayPvs(console.BeepSound, consUid);
                console.BeepCooldownTimer = console.BeepCooldown;
            }
        }
    }

    private void ResetConsoleUi(Entity<SupermatterConsoleComponent> entity, float frameTime)
    {
        entity.Comp.BeepCooldownTimer = TimeSpan.Zero;
        entity.Comp.UiUpdateTimer -= TimeSpan.FromSeconds(frameTime);

        if (frameTime != 0f && entity.Comp.UiUpdateTimer > TimeSpan.Zero)
            return;

        entity.Comp.UiUpdateTimer = entity.Comp.UiUpdateInterval;
        if (!_uiSystem.IsUiOpen(entity.Owner, SupermatterConsoleUiKey.Key))
            return;

        var emptyState = new SupermatterConsoleBuiState(activated: false);
        _uiSystem.SetUiState(entity.Owner, SupermatterConsoleUiKey.Key, emptyState);
    }

    private static (float integrity, SupermatterIntegrityLevel integrityLevel)
        CalculateIntegrity(SupermatterIntegrityComponent component)
    {
        var integrity = component.Integrity / component.MaxIntegrity * 100f;

        var ordered = component.SupermatterIntegrity.OrderByDescending(e => e.Threshold).ToList();
        var idx = ordered.FindIndex(entry => integrity >= entry.Threshold);

        var level = idx >= 0
            ? ordered[idx]
            : component.SupermatterIntegrity.MinBy(e => e.Threshold) ?? new SupermatterIntegrityLevel();

        return (integrity, level);
    }
}
