using Content.Server.Imperial.SCP.NothingThere.Components;
using Content.Shared.Imperial.SCP.NothingThere.Events;
using Content.Shared.Mobs;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Gibbing;
namespace Content.Server.Imperial.SCP.NothingThere.Systems;

public sealed partial class ImperialNothingThereSystem
{
    private void InitializeBodyControl()
    {
        SubscribeLocalEvent<ImperialControlledNothingThereComponent, MobStateChangedEvent>(OnControlledMobStateChanged);
        SubscribeLocalEvent<ImperialNothingThereComponent, ImperialNothingThereEnterBodyEvent>(OnEnterBodyAction);
        SubscribeLocalEvent<ImperialNothingThereComponent, ImperialNothingThereEnterBodyDoAfterEvent>(OnEnterBodyDoAfter);
        SubscribeLocalEvent<ImperialControlledNothingThereComponent, ImperialNothingThereGibBodyEvent>(OnGibBodyAction);
        SubscribeLocalEvent<ImperialControlledNothingThereComponent, ComponentInit>(OnInitControlled);
    }
    private void OnInitControlled(EntityUid uid, ImperialControlledNothingThereComponent comp, ComponentInit args)
    {
        if (comp.GibBodyEntity == null)
        {
            _actions.AddAction(uid,
                ref comp.GibBodyEntity,
                comp.GibBodyAction);
        }
    }
    private void OnEnterBodyAction(Entity<ImperialNothingThereComponent> ent, ref ImperialNothingThereEnterBodyEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (target == ent.Owner)
            return; // no dude
        if (!TryComp<MobStateComponent>(target, out var mobstate))
        {
            _popupSystem.PopupEntity(
                Loc.GetString("nothingthere-hammaggotson-notalive"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }
        if (mobstate.CurrentState != MobState.Dead)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("nothingthere-hammaggotson-notdead"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }
        if (HasComp<RottingComponent>(target))
        {
            _popupSystem.PopupEntity(
                Loc.GetString("nothingthere-hammaggotson-rotting"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }
        if (!HasComp<HumanoidProfileComponent>(target))
        {
            _popupSystem.PopupEntity(
                Loc.GetString("nothingthere-hammaggotson-nonhuman"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.EnterBodyWindUp, new ImperialNothingThereEnterBodyDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });
        var othersMessage = Loc.GetString("nothingthere-hammaggotson-windup-others");
        _popupSystem.PopupEntity(
                othersMessage,
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
    }

    private void OnEnterBodyDoAfter(Entity<ImperialNothingThereComponent> entity, ref ImperialNothingThereEnterBodyDoAfterEvent args)
    {
        var target = args.Target ?? EntityUid.Invalid;
        if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
            return;
        if (args.Cancelled || args.Handled || entity.Comp.Deleted)
            return;
        if (!TryComp<ImperialNothingThereComponent>(args.User, out var comp))
            return;
        if (!TryComp<MobThresholdsComponent>(target, out var thrd))
            return;
        if (!TryComp<MobStateComponent>(target, out var mob))
            return;
        var control = EnsureComp<ImperialControlledNothingThereComponent>(target);
        if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var hand in hands.Hands.Keys)
            {
                _hands.TryDrop((target, hands), hand!);
            }
            RemComp<HandsComponent>(target);
        }
        if (!_mobThresholdSystem.TryGetDeadThreshold(target, out var ddde))
            return;
        RemComp<SlowOnDamageComponent>(target);
        var nddde = ddde + comp.Threshold ?? FixedPoint2.New(400.0f);
        _mobThresholdSystem.SetMobStateThreshold(target, nddde, MobState.Dead, thrd);
        _mobThresholdSystem.SetMobStateThreshold(target, FixedPoint2.MaxValue, MobState.Critical, thrd);
        _mobStateSystem.ChangeMobState(target, MobState.Alive, mob, args.User);
        _mind.TransferTo(mindId, target);
        control.KillCount = comp.KillCount;
        control.KillCount++;
        if (_playerManager.TryGetSessionById(mind.UserId, out var sessionscp) && control.KillCount == comp.KillsRequired)
        {
            var message = Loc.GetString("nothingthere-hammaggotson-haha");
            _chatM.ChatMessageToOne(ChatChannel.Server, message, Loc.GetString("chat-manager-server-wrap-message", ("message", message)), default, false, sessionscp.Channel);
        }
        _audio.PlayPvs(comp.EnterSound, target);
        var transformA = Transform(args.User);
        EnsurePausedMap();
        StopChaseMusic(entity.Owner, entity.Comp);
        _transform.SetParent(args.User, transformA, PausedMap!.Value);
        control.OriginalBody = args.User;
    }

    private void OnControlledMobStateChanged(EntityUid uid, ImperialControlledNothingThereComponent comp, MobStateChangedEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        if (_mobStateSystem.IsDead(uid) || _mobStateSystem.IsCritical(uid))
        {
            var transformB = Transform(uid);
            var coords = transformB.Coordinates;
            var newb = comp.OriginalBody;
            var transformA = Transform(newb);
            _transform.SetParent(newb, transformA, transformB.ParentUid);
            _transform.SetCoordinates(newb, transformA, transformB.Coordinates, transformB.LocalRotation);
            _mind.TransferTo(mindId, newb);
            _audio.PlayPvs(comp.ExitSound, newb);
            Gib(uid, comp.GibSound, comp.GibletLaunchImpulse, comp.GibletLaunchImpulseVariance, true);
            if (TryComp<ImperialNothingThereComponent>(newb, out var nt))
            {
                nt.KillCount = comp.KillCount;
                StartChaseMusic(newb, nt);
            }
        }
    }
    public HashSet<EntityUid> Gib(EntityUid ent, SoundSpecifier gibSound, float gibletLaunchImpulse, float gibletLaunchImpulseVariance, bool dropGiblets = true, EntityUid? user = null)
    {
        if (!_destructible.DestroyEntity(ent))
            return new();

        _audio.PlayPvs(gibSound, ent);

        var gibbed = new HashSet<EntityUid>();
        var beingGibbed = new BeingGibbedEvent(gibbed);
        RaiseLocalEvent(ent, ref beingGibbed);

        if (dropGiblets)
        {
            foreach (var giblet in gibbed)
            {
                _transform.DropNextTo(giblet, ent);
                FlingDroppedEntity(giblet, gibletLaunchImpulse, gibletLaunchImpulseVariance);
            }
        }

        var beforeDeletion = new GibbedBeforeDeletionEvent(gibbed);
        RaiseLocalEvent(ent, ref beforeDeletion);

        return gibbed;
    }

    private void FlingDroppedEntity(EntityUid target, float gibletLaunchImpulse, float gibletLaunchImpulseVariance)
    {
        var impulse = gibletLaunchImpulse + _random.NextFloat(gibletLaunchImpulseVariance);
        var scatterVec = _random.NextAngle().ToVec() * impulse;
        _physics.ApplyLinearImpulse(target, scatterVec);
    }
    private void OnGibBodyAction(Entity<ImperialControlledNothingThereComponent> ent, ref ImperialNothingThereGibBodyEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<MobStateComponent>(args.Performer, out var mob))
            return;
        args.Handled = true;
        _mobStateSystem.ChangeMobState(args.Performer, MobState.Dead, mob, args.Performer);
    }
}
