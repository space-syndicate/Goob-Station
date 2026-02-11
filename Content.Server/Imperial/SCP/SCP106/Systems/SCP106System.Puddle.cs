using Content.Server.Imperial.SCP.SCP106.Components;
using Content.Shared.Imperial.SCP.SCP106.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Bed.Sleep;
using Content.Shared.Popups;
using Content.Shared.Chat.Prototypes;
namespace Content.Server.Imperial.SCP.SCP106.Systems;

public sealed partial class SCP106System
{
    #region Puddle Startup

    private void InitializePuddle()
    {
        SubscribeLocalEvent<SCP106PuddleComponent, StartCollideEvent>(OnPuddleCollide);
        SubscribeLocalEvent<SCP106PuddleComponent, ComponentStartup>(OnPuddleInit);
    }
    private void OnPuddleInit(EntityUid uid, SCP106PuddleComponent component, ComponentStartup args)
    {
        //Whener a puddle is spawned (e.g. via admin panel of some sort), it automatically is configured to teleport to the dimension, if one exists. Otherwise, it does nothing
        var query = EntityQueryEnumerator<SCP106PocketDimensionMarkerComponent>();
        while (query.MoveNext(out var entity, out var comp))
        {
            component.TargetMap = Transform(entity).MapID;
            break; // we only need one of them so yeah, i am extremely sorry for this
        }
        foreach (var scp in EntityQuery<SCP106Component>())
        {
            if (!scp.Puddles.Contains(uid))
                scp.Puddles.Add(uid);
        }
    }

    #endregion

    #region Colliding

    private void OnPuddleCollide(EntityUid uid, SCP106PuddleComponent component, ref StartCollideEvent args)
    {

        var subject = args.OtherEntity;

        if (TryComp<SCP106Component>(subject, out var scpcomp))
            return;
        if (Transform(subject).Anchored)
            return;
        if (TryComp<SCP106PuddleImmunityComponent>(subject, out var imm))
            return;
        if (TryComp<PullableComponent>(subject, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(subject, pullable);
        }
        if (TryComp<PullerComponent>(subject, out var pullerComp)
            && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
        {
            _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling);
        }
        if (TryComp<SCP106PuddleStunComponent>(subject, out var idksterling))
            return;
        if (TryComp<MobStateComponent>(subject, out var mobstate))
        {
            if (component.TargetMap == MapId.Nullspace)
                return;

            TryPuddleStun(subject, uid, component.PrototypeScream, component.TeleportSound);
        }
    }

    #endregion

    #region Puddle stun handling
    private void TryPuddleStun(EntityUid subject, EntityUid uid, ProtoId<EmotePrototype> emote, SoundSpecifier sound)
    {
        //It stuns, then adds a component, that after a certain duration teleports owner of the stun to the dimension (also putting to sleep)
        _stun.TryAddStunDuration(subject, TimeSpan.FromSeconds(4));
        _chat.TryEmoteWithChat(subject, emote, ignoreActionBlocker: true);
        var stunc = EnsureComp<SCP106PuddleStunComponent>(subject);
        _blindableSystem.UpdateIsBlind(subject);
        var curTime = _gameTiming.CurTime;
        var endTime = curTime + stunc.Delay;
        stunc.StunEnd = endTime;
        stunc.Puddle = uid;
        _godmode.ToggleGodmode(subject); //I was asked to do it, so the person won't get killed why he gets sucked to doom!!
        _audio.PlayPredicted(sound, subject, uid);
    }

    private void UpdatePuddleStun()
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SCP106PuddleStunComponent>();
        while (query.MoveNext(out var uid, out var stun))
        {
            if (curTime < stun.StunEnd)
                continue;
            if (!TryComp<SCP106PuddleComponent>(stun.Puddle, out var puddl))
                continue;
            if (puddl.TargetMap == null)
                continue;
            TeleportEntity(stun.Puddle, uid, puddl.TargetMap ?? MapId.Nullspace, puddl.GlobalTeleportSound, puddl.DamagePerSecond);
            var duration = TimeSpan.FromSeconds(5);
            _blindableSystem.UpdateIsBlind(uid);
            RemComp<SCP106PuddleStunComponent>(uid);
            _godmode.ToggleGodmode(uid);
            _statusEffects.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, duration);
        }
    }
    #endregion
    #region Puddle Placement
    private void OnPuddleAction(SCP106SpawnPuddleActionEvent args)
    {
        //This action spawns a puddle underneath you, if it passed some checks
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        var coords = Transform(args.Performer).Coordinates;
        if (IsTooCloseToPuddles(scp, coords, scp.MinDistance) != EntityUid.Invalid) //It can't be too close to other puddles for balancing issues
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-tooclose"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        if (IsOnFloor(coords) == false || NoWallsOrWindowsUnderneath(coords) == false || IsTooCloseToWeakling(coords, args.Performer)) // It can't be in space, it can't be on walls/windows/etc (but it can be on arcade machines, etc!), and it can't be right under a human!
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-dumbaz"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        var doAfterArgs = new DoAfterArgs(EntityManager,
        args.Performer,
        scp.Delay,
        new SCP106DoAfterPuddleEvent(),
        eventTarget: args.Performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs); //We don't want SCP 106 to place puddles right away!
        args.Handled = true;
    }

    private void OnPuddleDoAfter(Entity<SCP106Component> entity, ref SCP106DoAfterPuddleEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted)
            return;
        if (!TryComp<SCP106Component>(args.User, out var scp))
            return;
        if (scp.Puddles.Count >= scp.MaxPuddles)
        {
            if (scp.CanPlaceOverLimit == false)
            {
                return;
            }
        }
        var coords = Transform(args.User).Coordinates;
        if (IsTooCloseToPuddles(scp, coords, scp.MinDistance) != EntityUid.Invalid) //WE check again in case of some extra forces spawning a puddle
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-tooclose"),
                args.User,
                args.User,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        var puddle = Spawn(scp.PuddleID, coords);
        if (TryComp<SCP106PuddleComponent>(puddle, out var puddleComp))
            puddleComp.TargetMap = scp.PocketMapId; //We configure the puddle to actually teleport when colliding with it
        scp.Puddles.Add(puddle);
        //We have a maximum amount of puddles, so if we go over it, then we basically delete one.
        if (scp.Puddles.Count > scp.MaxPuddles && scp.Puddles.Count > 0)
        {
            var oldestPuddle = scp.Puddles[0];
            if (Exists(oldestPuddle))
            {
                scp.Puddles.RemoveAt(0);
                QueueDel(oldestPuddle);
            }
        }
        _audio.PlayPredicted(scp.PuddleSpawnSound, args.User, puddle, AudioParams.Default.WithVolume(2f));
    }

    private void OnDestroyPuddleAction(SCP106DestroyPuddleActionEvent args)
    {
        //We destroy the puddle right beneath us!
        var user = args.Performer;
        if (args.Handled || !TryComp<SCP106Component>(user, out var scp))
            return;
        var coords = Transform(user).Coordinates;
        var target = IsTooCloseToPuddles(scp, coords, scp.PuddleExitDistance); //1 tile away by default
        if (target == EntityUid.Invalid)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-pefar"),
                user,
                user,
                PopupType.MediumCaution);
            return;
        }
        if (scp.Puddles.Contains(target)) //we as well delete it from the list so things won't happen!
        {
            scp.Puddles.Remove(target);
            QueueDel(target);
        }
        args.Handled = true;
    }

    public void RemoveAllPuddles()
    {
        foreach (var scp in EntityQuery<SCP106Component>())
        {
            foreach (var puddle in scp.Puddles)
            {
                if (Exists(puddle))
                {
                    QueueDel(puddle);
                }
            }
            scp.Puddles.Clear();
        }
    }
    #endregion

    #region Puddle Transmission
    private void OnTransmissionPuddleAction(SCP106TransmissionPuddleActionEvent args)
    {
        //When we press this near a puddle we begin a do after
        //After it finishes, if we were in physical form, we go into "ghost" form (non-physical), where we fly as an AI and can interact with puddles and stuff
        //Otherwise if we were in that ghost form we exit it and enter our physical body again
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        var coords = Transform(args.Performer).Coordinates;
        var user = args.Performer;
        var target = IsTooCloseToPuddles(scp, coords, scp.PuddleExitDistance);
        PuddleTransmission(target, args.Performer, scp);
    }

    private void PuddleTransmission(EntityUid target, EntityUid user, SCP106Component scp)
    {
        if (target == EntityUid.Invalid)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-pefar"),
                user,
                user,
                PopupType.MediumCaution);
            return;
        }
        var doAfterArgs = new DoAfterArgs(EntityManager,
        user,
        scp.Delay,
        new SCP106DoAfterGhostPuddleEvent(),
        eventTarget: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        if (scp.InDimension == true)
        {
            _popupSystem.PopupEntity(Loc.GetString("scp106-hammaggotson-exit"), target, PopupType.SmallCaution);
        }
    }

    private void OnGhostPuddleDoAfter(Entity<SCP106Component> entity, ref SCP106DoAfterGhostPuddleEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted)
            return;
        if (!TryComp<SCP106Component>(args.User, out var scp))
            return;
        if (scp.InDimension == false)
        {
            var newb = TransformInto(args.User, scp.GhostMorph);
            var newscp = EnsureComp<SCP106Component>(newb);
            newscp.InDimension = true;
            _actions.RemoveAction(newb, newscp.PuddleEnterPuddleEntity); //IN CASE IT GETS ADDED
        }
        else
        {
            var newb = TransformInto(args.User, scp.PhysicalMorph);
            var newscp = EnsureComp<SCP106Component>(newb);
            newscp.InDimension = false;
            _audio.PlayPvs(newscp.ExitSound, newb);
        }
    }
    #endregion
}
