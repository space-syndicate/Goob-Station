using Content.Client._CorvaxGoob.Clothing.Components;
using Content.Client.Alerts;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Content.Shared.Zombies;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._CorvaxGoob.Clothing;

public sealed class SuitBreathingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;
    [Dependency] private readonly ClientAlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> UndeadTag = "Undead";

    private EntityUid? _activeStream;
    private Entity<SuitBreathingComponent>? _activeHelmet;
    private bool _isEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _alerts.SyncAlerts += OnSyncAlerts;
        _alerts.ClearAlerts += OnClearAlerts;

        SubscribeLocalEvent<SuitBreathingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SuitBreathingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<SuitBreathingComponent, ComponentShutdown>(OnHelmetShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _alerts.SyncAlerts -= OnSyncAlerts;
        _alerts.ClearAlerts -= OnClearAlerts;
    }

    private void OnSyncAlerts(object? sender, IReadOnlyDictionary<AlertKey, AlertState> alerts)
    {
        if (_player.LocalEntity is { } localPlayer)
            UpdateBreathingState(localPlayer);
    }

    private void OnClearAlerts(object? sender, EventArgs args)
    {
        StopBreathing();
    }

    private void OnGotEquipped(Entity<SuitBreathingComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Equipee == _player.LocalEntity)
            UpdateBreathingState(args.Equipee);
    }

    private void OnGotUnequipped(Entity<SuitBreathingComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.Equipee == _player.LocalEntity)
            StopBreathing();
    }

    private void OnHelmetShutdown(Entity<SuitBreathingComponent> ent, ref ComponentShutdown args)
    {
        if (_activeHelmet?.Owner == ent.Owner)
            StopBreathing();
    }

    private void UpdateBreathingState(EntityUid player)
    {
        if (!_inventory.TryGetSlotEntity(player, "head", out var headUid) ||
            !TryComp<SuitBreathingComponent>(headUid, out var breathingComp) ||
            !CanPlayBreathing(player, (headUid.Value, breathingComp)))
        {
            StopBreathing();
            return;
        }

        _activeHelmet = (headUid.Value, breathingComp);
        _isEnabled = true;
    }

    private void StopBreathing()
    {
        _activeStream = _audio.Stop(_activeStream);

        if (_activeHelmet is { } helmet)
            helmet.Comp.Timer = TimeSpan.Zero;

        _activeHelmet = null;
        _isEnabled = false;
    }

    private bool CanPlayBreathing(EntityUid player, Entity<SuitBreathingComponent> helmet)
    {
        if (!CanBreathe(player))
            return false;

        if (!TryComp<BreathToolComponent>(helmet, out var breathTool) ||
            !breathTool.IsFunctional ||
            !_internals.AreInternalsWorking(player) ||
            !_alerts.TryGetAlertState(player, AlertKey.ForCategory("Internals"), out var internalsAlert) ||
            internalsAlert.Severity != 1 ||
            _alerts.IsShowingAlertCategory(player, "Breathing"))
        {
            return false;
        }

        return true;
    }

    private bool CanBreathe(EntityUid uid)
    {
        return !HasComp<SiliconComponent>(uid) &&
               !HasComp<BreathingImmunityComponent>(uid) &&
               !HasComp<ZombieComponent>(uid) &&
               !HasComp<DebrainedComponent>(uid) &&
               !_tag.HasTag(uid, UndeadTag) &&
               !_mobState.IsIncapacitated(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || !_isEnabled)
            return;

        if (_activeHelmet is not { } helmet || _timing.CurTime < helmet.Comp.Timer)
            return;

        if (_player.LocalEntity is not { } localPlayer || !CanPlayBreathing(localPlayer, helmet))
        {
            StopBreathing();
            return;
        }

        if (Exists(_activeStream))
            return;

        helmet.Comp.Timer = _timing.CurTime + _random.Next(helmet.Comp.MinInterval, helmet.Comp.MaxInterval);

        if (helmet.Comp.BreathingSound != null)
        {
            _activeStream = _audio.PlayLocal(
                helmet.Comp.BreathingSound,
                localPlayer,
                localPlayer)?.Entity;
        }
    }
}
