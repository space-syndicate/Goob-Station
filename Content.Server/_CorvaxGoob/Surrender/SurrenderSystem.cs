using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Inventory;
using Content.Shared._CorvaxGoob.Hostage;
using Content.Shared._CorvaxGoob.Surrender;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._CorvaxGoob.Surrender;

public sealed partial class SurrenderSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly VirtualItemSystem _virtual = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurrenderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SurrenderComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SurrenderComponent, StopSurrendDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SurrenderComponent, SurrenderAlertEvent>(OnSurronderAlert);
        SubscribeLocalEvent<SurrenderComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDoAfter(Entity<SurrenderComponent> entity, ref StopSurrendDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (HasComp<HostageComponent>(entity))
            return;

        RemComp<SurrenderComponent>(entity);
        _modifier.RefreshMovementSpeedModifiers(entity);
    }

    private void OnMobStateChanged(Entity<SurrenderComponent> entity, ref MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Alive)
            return;

        RemComp<SurrenderComponent>(entity);
    }
    private void OnSurronderAlert(Entity<SurrenderComponent> entity, ref SurrenderAlertEvent args)
    {
        if (entity.Comp.SurrendEffectUid is not { } effect)
            return;

        var ev = new TryStopSurrendingEvent();

        RaiseLocalEvent(entity, ref ev);
        if (ev.Cancelled)
            return;

        if (entity.Comp.StopSurrendDoAfter is null)
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, entity, entity.Comp.StopSurrendTime, new StopSurrendDoAfterEvent(), entity)
            {
                BlockDuplicate = true,
                BreakOnDamage = false,
                BreakOnMove = false,
                RequireCanInteract = false
            }, out var doAfterId);
            entity.Comp.StopSurrendDoAfter = doAfterId;
        }
        else
        {
            _doAfter.Cancel(entity.Comp.StopSurrendDoAfter);
            entity.Comp.StopSurrendDoAfter = null;
        }

        if (TryComp<AppearanceComponent>(effect, out var appearance))
            _appearance.SetData(effect, StopSurrendingStatus.StopSurrending, entity.Comp.StopSurrendDoAfter is not null, appearance);
    }

    private void OnComponentInit(Entity<SurrenderComponent> entity, ref ComponentInit ev)
    {
        if (HasComp<PacifiedComponent>(entity))
            entity.Comp.HadPacifismBefore = true;
        EnsureComp<PacifiedComponent>(entity);

        _alerts.ShowAlert(entity.Owner, entity.Comp.Alert);
        _modifier.RefreshMovementSpeedModifiers(entity);

        var effect = SpawnAtPosition(entity.Comp.SurrendEffectProto, Transform(entity).Coordinates);
        _xform.SetParent(effect, entity);
        entity.Comp.SurrendEffectUid = effect;

        _audio.PlayPvs(entity.Comp.SurrendSound, entity);

        if (TryComp<HandsComponent>(entity, out var hands))
            foreach (var hand in _hands.EnumerateHands((entity, hands)))
                _hands.TryDrop(entity.Owner, hand);
    }

    private void OnComponentShutdown(Entity<SurrenderComponent> entity, ref ComponentShutdown ev)
    {
        if (HasComp<PacifiedComponent>(entity) && !entity.Comp.HadPacifismBefore)
            RemComp<PacifiedComponent>(entity);

        _alerts.ClearAlert(entity.Owner, entity.Comp.Alert);

        if (entity.Comp.SurrendEffectUid is null)
            return;

        _virtual.DeleteInHandsMatching(entity.Comp.SurrendEffectUid.Value, entity, true);

        QueueDel(entity.Comp.SurrendEffectUid);
    }
}
