using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Events;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Imperial.Power.Systems;

public sealed class SharedSupermatterIntegritySystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = null!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterIntegrityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SupermatterIntegrityComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterIntegrityComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SupermatterIntegrityComponent, SupermatterShutdownDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SupermatterIntegrityComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnInit(Entity<SupermatterIntegrityComponent> ent, ref ComponentInit args)
    {
        var ev = new SupermatterStartupEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnStartCollide(Entity<SupermatterIntegrityComponent> entity, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!_tagSystem.HasTag(other, entity.Comp.HealTag))
            return;

        if (!entity.Comp.Activated)
        {
            entity.Comp.Activated = true;
            Dirty(entity);

            var ev = new SupermatterSendRadioEvent(Loc.GetString("supermatter-activated"));
            RaiseLocalEvent(entity, ref ev);
        }

        entity.Comp.Integrity = MathF.Min(entity.Comp.MaxIntegrity, entity.Comp.Integrity + entity.Comp.EmitterHealAmount);
        Dirty(entity);
    }

    private void OnExamined(Entity<SupermatterIntegrityComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(entity.Comp.Activated
            ? $"[color=yellow]{Loc.GetString("supermatter-status-active")}[/color]"
            : $"[color=gray]{Loc.GetString("supermatter-status-inactive")}[/color]");

        var integrityPercent = entity.Comp.Integrity / entity.Comp.MaxIntegrity * 100;
        var integrityLevel = entity.Comp.SupermatterIntegrity.First(entry => integrityPercent >= entry.Threshold);

        args.PushMarkup(Loc.GetString(integrityLevel.Description));
    }

    private void OnInteractUsing(Entity<SupermatterIntegrityComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (!_tagSystem.HasTag(args.Used, entity.Comp.SupermatterStopTag)
            || args.Target == null)
            return;
        if (!entity.Comp.Activated)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 5, new SupermatterShutdownDoAfterEvent(), entity, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<SupermatterIntegrityComponent> entity, ref SupermatterShutdownDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!entity.Comp.Activated)
            return;

        _audioSystem.PlayPvs(entity.Comp.ShutdownSoundPath, entity);

        QueueDel(args.Used);
        entity.Comp.Activated = false;
        Dirty(entity);

        var ev = new SupermatterSendRadioEvent(Loc.GetString("supermatter-deactivated"));
        RaiseLocalEvent(entity, ref ev);
        args.Handled = true;
    }
}
