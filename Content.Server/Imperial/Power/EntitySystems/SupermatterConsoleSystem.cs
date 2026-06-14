using Content.Server.Atmos.EntitySystems;
using Content.Server.Imperial.Power.Components;
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
        return nearest;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SupermatterConsoleComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var console, out _))
        {
            var nearestUid = FindNearestSupermatter(uid);
            if (nearestUid == null || !TryComp<SupermatterIntegrityComponent>(nearestUid.Value, out var nearest))
            {
                console.BeepCooldownTimer = TimeSpan.Zero;

                console.UiUpdateTimer -= TimeSpan.FromSeconds(frameTime);
                if (console.UiUpdateTimer <= TimeSpan.Zero)
                {
                    console.UiUpdateTimer = console.UiUpdateInterval;
                    if (_uiSystem.IsUiOpen(uid, SupermatterConsoleUiKey.Key))
                    {
                        var emptyState = new SupermatterConsoleBuiState(
                            activated: false
                        );
                        _uiSystem.SetUiState(uid, SupermatterConsoleUiKey.Key, emptyState);
                    }
                }
                continue;
            }

            var supermatterEv = "—";
            if (TryComp<SupermatterEventComponent>(nearestUid.Value, out var eventComponent))
                supermatterEv = Loc.GetString($"supermatter-event-{eventComponent.CurrentEvent.ToString().ToLowerInvariant()}-name");

            var (integrityPercent, level) = CalculateIntegrity(nearest);
            var integrity = (int)Math.Round(integrityPercent);

            console.UiUpdateTimer -= TimeSpan.FromSeconds(frameTime);
            if (console.UiUpdateTimer <= TimeSpan.Zero)
            {
                console.UiUpdateTimer = console.UiUpdateInterval;

                if (_uiSystem.IsUiOpen(uid, SupermatterConsoleUiKey.Key))
                {
                    var transComp = Transform(nearestUid.Value);
                    var gas = _atmosSystem.GetContainingMixture((nearestUid.Value, transComp));

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

                    _uiSystem.SetUiState(uid, SupermatterConsoleUiKey.Key, state);
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
                _audioSystem.PlayPvs(console.BeepSound, uid);
                console.BeepCooldownTimer = console.BeepCooldown;
            }
        }
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
