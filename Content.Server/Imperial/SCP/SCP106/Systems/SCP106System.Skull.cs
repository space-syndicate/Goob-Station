using Content.Server.Imperial.SCP.SCP106.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Bed.Sleep;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Imperial.SCP.SCP106.Systems;

public sealed partial class SCP106System
{
    #region Collision handling
    private void OnSkullCollide(EntityUid uid, SCP106SkullComponent component, ref StartCollideEvent args)
    {
        //when we touch a skull, we get teleported to a random puddle, and we are granted puddle immunity
        var subject = args.OtherEntity;

        if (TryComp<SCP106Component>(subject, out var scpcomp))
            return;
        if (Transform(subject).Anchored)
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
        if (TryComp<MobStateComponent>(subject, out var mobstate))
        {
            var allPuddls = new HashSet<EntityUid>();
            var query = EntityQueryEnumerator<SCP106PuddleComponent>();
            while (query.MoveNext(out var uidd, out var pddl))
            {
                allPuddls.Add(uidd);
            }
            var transform = _entityManager.GetComponent<TransformComponent>(subject);
            if (allPuddls.Count == 0)
            {
                _transform.SetWorldPosition((subject, transform), new Vector2(0, 0));
                if (TryComp<SCP106DimensionDebuffComponent>(subject, out var ddb) && _mapSystem.TryGetMap(ddb.PastMapId, out var mapEnt1))
                {
                    _transform.SetParent(subject, transform, mapEnt1.Value);
                    RemComp<SCP106DimensionDebuffComponent>(subject);
                    return;
                }
            }
            var puddl = _random.Pick(allPuddls);
            var ourCoords = Transform(puddl).Coordinates;

            var transformPuddl = _entityManager.GetComponent<TransformComponent>(puddl);
            if (_mapSystem.TryGetMap(transformPuddl.MapID, out var mapEnt))
            {
                var imm = EnsureComp<SCP106PuddleImmunityComponent>(subject);
                var curTime = _gameTiming.CurTime;
                var endTime = curTime + component.Duration;
                imm.ImmunityEnd = endTime;
                _transform.SetCoordinates(subject, ourCoords);
                _transform.SetParent(subject, transform, mapEnt.Value);
                RemComp<SCP106DimensionDebuffComponent>(subject);
                _statusEffects.TryRemoveStatusEffect(subject, SleepingSystem.StatusEffectForcedSleeping); //IF HE IS ASLEEP FOR SOME REASON
                if (TryComp<SleepingComponent>(subject, out var sleeep))
                {
                    _sleep.TryWaking(new Entity<SleepingComponent?>(subject, sleeep), true);
                }
                _audio.PlayPvs(component.ExitSound, puddl);
            }
        }
    }
    #endregion
    #region Puddle Immunity Update
    private void UpdatePuddleImmunity()
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SCP106PuddleImmunityComponent>();
        while (query.MoveNext(out var entity, out var imm))
        {
            if (curTime < imm.ImmunityEnd)
                continue;
            RemComp<SCP106PuddleImmunityComponent>(entity);
        }
    }
    #endregion
}
