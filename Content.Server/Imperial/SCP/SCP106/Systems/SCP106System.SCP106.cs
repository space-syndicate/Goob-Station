using System.Linq;
using Content.Server.Imperial.SCP.SCP106.Components;
using Content.Shared.Imperial.SCP.SCP106.Events;
using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.EntitySerialization;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Robust.Shared.Random;
namespace Content.Server.Imperial.SCP.SCP106.Systems;

public sealed partial class SCP106System
{
    #region Component Startup

    private void InitializeSCP106()
    {
        SubscribeLocalEvent<SCP106Component, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<SCP106Component, ComponentStartup>(OnInit);
        SubscribeLocalEvent<SCP106SpawnPuddleActionEvent>(OnPuddleAction);
        SubscribeLocalEvent<SCP106Component, SCP106DoAfterPuddleEvent>(OnPuddleDoAfter);
        SubscribeLocalEvent<SCP106Component, SCP106DoAfterGhostPuddleEvent>(OnGhostPuddleDoAfter);
    }
    private void OnInit(EntityUid uid, SCP106Component component, ComponentStartup args)
    {
        //Adding all already existing puddles, in case of SCP 106 being respawned if he dissapears for some reason, so he would keep all his puddles! How convenient!
        if (!component.Puddles.Any())
        {
            component.Puddles = new();
            var query = EntityQueryEnumerator<SCP106PuddleComponent>();
            while (query.MoveNext(out var uiddd, out var puddll))
            {
                component.Puddles.Add(uid);
            }
        }

        //This code chunk is designated to loading up the map and initing it and stuff...
        if (component.PocketMapPath is { } path && component.PocketMapId == null)
        {
            var opts = DeserializationOptions.Default with { InitializeMaps = true }; //We want the map inited yeah
            //I know how bad this is, but what we are doing in this chunk of code is running through ALL markers, and if there is one, then we can find the map of dimension via it!
            var query = EntityQueryEnumerator<SCP106PocketDimensionMarkerComponent>();
            var markerEntity = EntityUid.Invalid;
            SCP106PocketDimensionMarkerComponent? dimensionMarker = null;
            while (query.MoveNext(out var entity, out var comp))
            {
                markerEntity = entity;
                dimensionMarker = comp;
                break; // we only need one of them so yeah, i am extremely sorry for this
            }
            var spwarp = true;
            if (dimensionMarker != null) //If there is a dimension marker, we just grab the map ID of where it is and we are fine!
            {
                spwarp = false;
                var trs = Transform(markerEntity);
                component.PocketMapId = trs.MapID;
            }
            else if (_mapLoader.TryLoadMap(path, out var map, out var gridSet, opts)) //Otherwise we just load it
            {
                component.PocketMapId = map.Value.Comp.MapId;
            }
            else
            {
                Log.Error($"Failed to load SCP 106 dimension map from {path}!");
                return;
            }
            if (_mapSystem.TryGetMap(component.PocketMapId, out var mapEnt) & spwarp) //We are placing a new dimension marker if we loaded a new map, so we can use it in future!
            {
                var mapEntNN = mapEnt ?? EntityUid.Invalid;
                var warp = Spawn(component.WarpID);
                var transform = _entityManager.GetComponent<TransformComponent>(warp);
                _transform.SetWorldPosition((warp, transform), new Vector2(0, 0));
                _transform.SetParent(warp, transform, mapEntNN);
            }
        }

        //Loading up the actions
        if (component.PuddleSpawnActionEntity == null)
        {
            _actions.AddAction(uid,
                ref component.PuddleSpawnActionEntity,
                component.PuddleSpawnAction);
        }
        if (component.InPocketDimension == false)
        {
            if (component.InDimension == false)
            {
                _actions.AddAction(uid,
                    ref component.PuddleEnterPuddleEntity,
                    component.PuddleEnterPuddleAction);
            }
            else
            {
                if (component.PuddleExitPuddleEntity == null)
                {
                    _actions.AddAction(uid,
                        ref component.PuddleExitPuddleEntity,
                        component.PuddleExitPuddleAction);
                }
                if (component.PuddleEnterDimensionEntity == null)
                {
                    _actions.AddAction(uid,
                        ref component.PuddleEnterDimensionEntity,
                        component.PuddleEnterDimensionAction);
                }
            }
            if (component.PuddleDestroyPuddleEntity == null)
            {
                _actions.AddAction(uid,
                    ref component.PuddleDestroyPuddleEntity,
                    component.PuddleDestroyPuddleAction);
            }
        }
        else
        {
            if (component.PuddleExitDimensionEntity == null)
            {
                _actions.AddAction(uid,
                    ref component.PuddleExitDimensionEntity,
                    component.PuddleExitDimensionAction);
            }
        }
    }
    #endregion
    #region Attack handling
    private void OnAttack(EntityUid entity, SCP106Component comp, ref MeleeHitEvent args)
    {
        //On attack if we hit the person only the first time, we deal additional damage to him. If we hit him second time in a row, we teleport him to the dimension (BUT IF WE AREN'T IN DIMENSION)
        //And then if we hit him in the dimension, we teleport him to one of the coords
        if (args.HitEntities is [])
            return;
        if (!TryComp<MobStateComponent>(args.HitEntities[0], out var mobstateCom))
        {
            return;
        }
        if (comp.InPocketDimension == false)
        {
            if (comp.LastAttackedPerson == args.HitEntities[0])
            {
                comp.LastAttackedPerson = EntityUid.Invalid;
                if (TryComp<PullerComponent>(args.HitEntities[0], out var pullerComp)
                    && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
                {
                    _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling);
                }
                var duration = TimeSpan.FromSeconds(5);
                _audio.PlayPredicted(comp.TeleportSound, entity, args.HitEntities[0]);
                TeleportEntity(entity, args.HitEntities[0], comp.PocketMapId ?? MapId.Nullspace, comp.GlobalTeleportSound, comp.DamagePerSecond);
                _audio.PlayPvs(comp.LaughSound, entity, AudioParams.Default.WithVolume(2f));
                _statusEffects.TryAddStatusEffectDuration(args.HitEntities[0], SleepingSystem.StatusEffectForcedSleeping, duration);
            }
            else
            {
                comp.LastAttackedPerson = args.HitEntities[0];
                _damageable.TryChangeDamage(args.HitEntities[0], comp.FirstHitDamage);
            }
        }
        else
        {
            if (TryComp<BodyComponent>(args.HitEntities[0], out var body))
            {
                _chat.TryEmoteWithChat(args.HitEntities[0], comp.PrototypeScream, ignoreActionBlocker: true); //SCREAM!!
                if (comp.SleepOnAttack == true)
                {
                    _statusEffects.TryAddStatusEffectDuration(args.HitEntities[0], SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(90.0f)); //This was debug, don't mind it
                }
                else
                {
                    _statusEffects.TryAddStatusEffectDuration(args.HitEntities[0], SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(5.0f));
                    var transform = _entityManager.GetComponent<TransformComponent>(args.HitEntities[0]);
                    var randn = _random.Pick(new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 }); //I am sorry
                    _transform.SetWorldPosition((args.HitEntities[0], transform), new Vector2(comp.RandomCoordinatesX[randn], comp.RandomCoordinatesY[randn])); //I am very sorry
                }
            }
        }
    }
    #endregion

    #region Dimension Transmission
    private void OnSwitchDimensionAction(SCP106DimensionSwitchActionEvent args)
    {
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        SwitchDimensions(args.Performer);
        if (scp.InPocketDimension == true)
        {
            var newb = TransformInto(args.Performer, scp.PhysicalDMorph);
            var newscp = EnsureComp<SCP106Component>(newb);
            _actions.RemoveAction(newb, newscp.PuddleSpawnActionEntity);
            _actions.RemoveAction(newb, newscp.PuddleEnterPuddleEntity);
            _actions.RemoveAction(newb, newscp.PuddleDestroyPuddleEntity);
        }
        else
        {
            var newb = TransformInto(args.Performer, scp.GhostMorph);
        }
        args.Handled = true;
    }
    private void SwitchDimensions(EntityUid user)
    {
        if (!TryComp<SCP106Component>(user, out var scp))
            return;
        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return;
        var coords = Transform(user).Coordinates;
        scp.InPocketDimension = !scp.InPocketDimension;
        if (scp.InPocketDimension == false)
        {
            var oldcords = scp.PastPosition ?? EntityCoordinates.Invalid;
            var oldmap = scp.PastMapId;
            scp.InDimension = true;
            scp.PastPosition = coords;
            if (!TryComp<TransformComponent>(user, out var transform))
                return;
            if (_mapSystem.TryGetMap(oldmap, out var mapEnt))
            {
                _transform.SetCoordinates(user, oldcords);
                _transform.SetParent(user, transform, mapEnt.Value);
            }
        }
        else
        {
            scp.PastPosition = coords;

            if (!TryComp<TransformComponent>(user, out var transform))
                return;
            scp.PastMapId = transform.MapID;
            if (_mapSystem.TryGetMap(scp.PocketMapId, out var mapEnt))
            {
                _transform.SetWorldPosition((user, transform), new Vector2(0, 0));
                _transform.SetParent(user, transform, mapEnt ?? EntityUid.Invalid);
            }
        }
    }
    #endregion
}
