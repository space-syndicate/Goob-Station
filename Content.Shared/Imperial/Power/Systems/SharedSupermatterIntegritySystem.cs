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
    [Dependency] private readonly SharedAudioSystem _audioSystem = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = null!;
    [Dependency] private readonly TagSystem _tagSystem = null!;

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

    private void OnStartCollide(Entity<SupermatterIntegrityComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!_tagSystem.HasTag(other, ent.Comp.HealTag))
            return;

        if (!ent.Comp.Activated)
        {
            ent.Comp.Activated = true;
            DirtyField(ent, ent.Comp, nameof(ent.Comp.Activated));

            var ev = new SupermatterSendRadioEvent(Loc.GetString("supermatter-activated"));
            RaiseLocalEvent(ent, ref ev);
        }

        ent.Comp.Integrity = MathF.Min(ent.Comp.MaxIntegrity, ent.Comp.Integrity + ent.Comp.HealAmount);
        DirtyField(ent, ent.Comp, nameof(ent.Comp.Integrity));
    }

    private void OnExamined(Entity<SupermatterIntegrityComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(ent.Comp.Activated
            ? Loc.GetString("supermatter-status-active")
            : Loc.GetString("supermatter-status-inactive"));

        var integrityPercent = ent.Comp.Integrity / ent.Comp.MaxIntegrity * 100;
        var integrityLevel = ent.Comp.SupermatterIntegrity.First(entry => integrityPercent >= entry.Threshold);

        args.PushMarkup(Loc.GetString(integrityLevel.Description));
    }

    private void OnInteractUsing(Entity<SupermatterIntegrityComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!_tagSystem.HasTag(args.Used, ent.Comp.SupermatterStopTag)
            || args.Target == null)
            return;
        if (!ent.Comp.Activated)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 5, new SupermatterShutdownDoAfterEvent(), ent, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<SupermatterIntegrityComponent> ent, ref SupermatterShutdownDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!ent.Comp.Activated)
            return;

        _audioSystem.PlayPvs(ent.Comp.StopSoundPath, ent);

        QueueDel(args.Used);
        ent.Comp.Activated = false;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.Activated));

        var ev = new SupermatterSendRadioEvent(Loc.GetString("supermatter-deactivated"));
        RaiseLocalEvent(ent, ref ev);
        args.Handled = true;
    }
}
