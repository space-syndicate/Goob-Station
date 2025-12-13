using System.Linq;
using Content.Shared.Actions;
using Content.Server.Imperial.SCP.SCP106.Components;
using Content.Shared.Imperial.SCP.SCP106.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Server.Player;
using Content.Shared.DoAfter;
using Content.Shared.Stunnable;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Timing;
using Content.Shared.Bed.Sleep;
using Content.Server.Chat.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Verbs;
using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Content.Shared.Chat;
using Content.Server.Chat.Managers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Damage.Systems;
namespace Content.Server.Imperial.SCP.SCP106.Systems;

public sealed class SCP106System : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatM = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGodmodeSystem _godmode = default!;
    [Dependency] private readonly SleepingSystem _sleep = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SCP106Component, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<SCP106Component, MapInitEvent>(OnInit);
        SubscribeLocalEvent<SCP106SpawnPuddleActionEvent>(OnPuddleAction);
        SubscribeLocalEvent<SCP106DimensionEnterActionEvent>(OnEnterDimensionAction);
        SubscribeLocalEvent<SCP106DimensionExitActionEvent>(OnExitDimensionAction);
        SubscribeLocalEvent<SCP106PuddleComponent, StartCollideEvent>(OnPuddleCollide);
        SubscribeLocalEvent<SCP106Component, SCP106DoAfterPuddleEvent>(OnPuddleDoAfter);
        SubscribeLocalEvent<SCP106Component, SCP106DoAfterGhostPuddleEvent>(OnGhostPuddleDoAfter);
        SubscribeLocalEvent<SCP106PuddleComponent, GetVerbsEvent<AlternativeVerb>>(AddPuddleVerb);
        SubscribeLocalEvent<SCP106SkullComponent, StartCollideEvent>(OnSkullCollide);
        SubscribeLocalEvent<SCP106PuddleComponent, MapInitEvent>(OnPuddleInit);
        SubscribeLocalEvent<SCP106ExitPuddleActionEvent>(OnExitPuddleAction);
        SubscribeLocalEvent<SCP106EnterPuddleActionEvent>(OnEnterPuddleAction);
        SubscribeLocalEvent<SCP106DestroyPuddleActionEvent>(OnDestroyPuddleAction);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePuddleStun();
        UpdatePuddleImmunity();
        UpdateDimensionDebuff();
    }
private bool IsTooCloseToWeakling(EntityCoordinates coords, EntityUid scp)
{
    var mapCoords = coords.ToMap(EntityManager, _transform);
    var radius = 1f;
    foreach (var mob in EntityQuery<MobStateComponent>())
    {
        var mobUid = mob.Owner;
        if (mobUid == scp)
            continue;
        if (mob.CurrentState == Shared.Mobs.MobState.Dead || 
            mob.CurrentState == Shared.Mobs.MobState.Critical)
            continue;
        if (!TryComp<TransformComponent>(mobUid, out var mobTransform))
            continue;
        if (mobTransform.MapID != mapCoords.MapId)
            continue;
        var mobPos = _transform.GetWorldPosition(mobTransform);
        var distance = (mapCoords.Position - mobPos).Length();
        if (distance <= radius)
        {
            return true;
        }
    }
    return false;
}
    private EntityUid IsTooCloseToPuddles(SCP106Component scp, EntityCoordinates targetCoords, float dis)
    {
        var mindis = dis;
        var mindisSquared = mindis * mindis;
        var targetMap = targetCoords.GetMapId(EntityManager);
        var targetPos = targetCoords.ToMap(EntityManager, _transform).Position;
        
        foreach (var puddle in scp.Puddles)
        {
            if (!Exists(puddle) || !TryComp<TransformComponent>(puddle, out var puddleTransform))
                continue;
                
            if (puddleTransform.MapID != targetMap)
                continue;
                
            var puddlePos = _transform.GetWorldPosition(puddle);
            var distanceSquared = (targetPos - puddlePos).LengthSquared();
            
            if (distanceSquared < mindisSquared)
            {
                return puddle; 
            }
        }
        
        return EntityUid.Invalid; 
    }
    private void UpdatePuddleStun()
    {
        var curTime = _gameTiming.CurTime;
        foreach (var stun in EntityQuery<SCP106PuddleStunComponent>())
        {
            if (curTime >= stun.StunEnd)
            {
                if (!TryComp<SCP106PuddleComponent>(stun.Puddle, out var puddl))
                    return;
                TeleportEntity(stun.Puddle, stun.Owner, puddl.TargetMap, puddl.GlobalTeleportSound, puddl.DamagePerSecond);
                var duration = TimeSpan.FromSeconds(5);
                _blindableSystem.UpdateIsBlind(stun.Owner);
                RemComp<SCP106PuddleStunComponent>(stun.Owner);
                _godmode.ToggleGodmode(stun.Owner);
                _statusEffects.TryAddStatusEffectDuration(stun.Owner, SleepingSystem.StatusEffectForcedSleeping, duration);
            }
        }
    }
    private void UpdateDimensionDebuff()
    {
        var curTime = _gameTiming.CurTime;
        foreach (var debuff in EntityQuery<SCP106DimensionDebuffComponent>())
        {
            if (curTime >= debuff.NextDamage)
            {
                _damageable.TryChangeDamage(debuff.Owner, debuff.DamagePerSecond);
                debuff.NextDamage += TimeSpan.FromSeconds(1.0f);
            }
        }
    }


    private void UpdatePuddleImmunity()
    {
        var curTime = _gameTiming.CurTime;
        foreach (var imm in EntityQuery<SCP106PuddleImmunityComponent>())
        {
            if (curTime >= imm.ImmunityEnd)
            {
                RemComp<SCP106PuddleImmunityComponent>(imm.Owner);
            }
        }
    }
    private void OnInit(EntityUid uid, SCP106Component component, MapInitEvent args)
    {
        component.Puddles = new();
        foreach (var puddll in EntityQuery<SCP106PuddleComponent>())
        {
            component.Puddles.Add(puddll.Owner);
        }
        if (component.PocketMapPath is { } path)
        {
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            var query = EntityQuery<SCP106PocketDimensionMarkerComponent>();
            var compYe = query.FirstOrDefault();
            var spwarp = true;
            if (compYe != null)
            {
                spwarp = false;
                var ownEnt = compYe.Owner;
                var trs = Transform(ownEnt);
                component.PocketMapId = trs.MapID;
            }
            else if (_mapLoader.TryLoadMap(path, out var map, out var gridSet, opts))
            {
                component.PocketMapId = map.Value.Comp.MapId;
            }
            else
            {
                Log.Error($"Failed to load map from {path}!");
                return;
            }
            if (_mapSystem.TryGetMap(component.PocketMapId, out var mapEnt) & spwarp)
            {
                var mapEntNN = mapEnt ?? EntityUid.Invalid;
                var warp = Spawn(component.WarpID);
                var transform = _entityManager.GetComponent<TransformComponent>(warp);
                _transform.SetWorldPosition((warp, transform), new Vector2(0, 0));
                _transform.SetParent(warp, transform, mapEntNN);
            }
        }
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
        }
        _actions.AddAction(uid,
            ref component.PuddleDestroyPuddleEntity,
            component.PuddleDestroyPuddleAction);
        Dirty(uid, component);
    }
    private void OnPuddleInit(EntityUid uid, SCP106PuddleComponent component, MapInitEvent args)
    {
            var query = EntityQuery<SCP106PocketDimensionMarkerComponent>();
            var compYe = query.FirstOrDefault();
            var spwarp = true;
            if (compYe != null && component.TargetMap != null)
            {
                spwarp = false;
                var ownEnt = compYe.Owner;
                var trs = Transform(ownEnt);
                component.TargetMap = trs.MapID;
            }
    }
    private void OnPuddleAction(SCP106SpawnPuddleActionEvent args)
    {
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        var coords = Transform(args.Performer).Coordinates;
        if (IsTooCloseToPuddles(scp, coords, scp.MinDistance) != EntityUid.Invalid)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-tooclose"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        if (IsOnFloor(coords) == false || HasOnlyDoorsOrNoWalls(coords) == false || IsTooCloseToWeakling(coords, args.Performer))
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
        TimeSpan.FromSeconds(scp.Delay),
        new SCP106DoAfterPuddleEvent(),
        eventTarget: args.Performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnPuddleDoAfter(Entity<SCP106Component> entity, ref SCP106DoAfterPuddleEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted)
            return;
        if (!TryComp<SCP106Component>(args.User, out var scp))
            return;
        var coords = Transform(args.User).Coordinates;
        if (IsTooCloseToPuddles(scp, coords, scp.MinDistance) != EntityUid.Invalid)
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
            puddleComp.TargetMap = scp.PocketMapId;
        scp.Puddles.Add(puddle);
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


            _stun.TryAddStunDuration(subject, TimeSpan.FromSeconds(4));

            _chat.TryEmoteWithChat(subject, component.PrototypeScream, ignoreActionBlocker: true);
            var stunc = EnsureComp<SCP106PuddleStunComponent>(subject);
            var curTime = _gameTiming.CurTime;
            var endTime = curTime + stunc.Delay;
            stunc.StunEnd = endTime;
            stunc.Puddle = uid;
            _audio.PlayPredicted(component.TeleportSound, subject, uid);
        }
    }

    private void TeleportEntity(EntityUid hole, EntityUid subject, MapId map, SoundSpecifier globaltpsound, DamageSpecifier damage)
    {


        var ourCoords = Transform(hole).Coordinates;
        var transform = _entityManager.GetComponent<TransformComponent>(subject);
        if (_mapSystem.TryGetMap(map, out var mapEnt))
        {
            if (_mind.TryGetMind(subject, out _, out var mindComponent))
            {
                if (_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
                {
                    _chatM.ChatMessageToOne(ChatChannel.Server, Loc.GetString("scp106-hammaggotson-urdamned"), Loc.GetString("chat-manager-server-wrap-message", ("message", Loc.GetString("scp106-hammaggotson-urdamned"))), default, false, session.Channel);
                }
            }
            var query = EntityQuery<SCP106Component>();
            var compYe = query.FirstOrDefault();
            if (compYe != null)
            {
                if (_mind.TryGetMind(compYe.Owner, out _, out var mindComponentscp))
                {
                    if (_playerManager.TryGetSessionById(mindComponentscp.UserId, out var sessionscp))
                    {
                        _chatM.ChatMessageToOne(ChatChannel.Server, Loc.GetString("scp106-hammaggotson-wildhunt", ("name", Comp<MetaDataComponent>(subject).EntityName)), Loc.GetString("chat-manager-server-wrap-message", ("message", Loc.GetString("scp106-hammaggotson-wildhunt", ("name", Comp<MetaDataComponent>(subject).EntityName)))), default, false, sessionscp.Channel);
                    }
                }
            }
            _transform.SetWorldPosition((subject, transform), new Vector2(0, 0));
            _transform.SetParent(subject, transform, mapEnt.Value);
            var dimensiondebuff = EnsureComp<SCP106DimensionDebuffComponent>(subject);
            dimensiondebuff.DamagePerSecond = damage;
            dimensiondebuff.NextDamage = _gameTiming.CurTime + TimeSpan.FromSeconds(1.0f);
            _godmode.ToggleGodmode(subject);
        }
        _audio.PlayGlobal(globaltpsound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
    }

    private void OnAttack(EntityUid entity, SCP106Component comp, ref MeleeHitEvent args)
    {
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
                if (TryComp<PullerComponent>(args.HitEntities[0], out var pullerComp)
                    && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
                {
                    _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling);
                }
                _audio.PlayPredicted(comp.TeleportSound, entity, args.HitEntities[0]);
                TeleportEntity(entity, args.HitEntities[0], comp.PocketMapId, comp.GlobalTeleportSound, comp.DamagePerSecond);
                _audio.PlayPvs(comp.LaughSound, entity, AudioParams.Default.WithVolume(2f));
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
                _chat.TryEmoteWithChat(args.HitEntities[0], comp.PrototypeScream, ignoreActionBlocker: true);
                if (comp.SleepOnAttack == true)
                {
                    _statusEffects.TryAddStatusEffectDuration(args.HitEntities[0], SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(90.0f));
                }
                else
                {
                    _statusEffects.TryAddStatusEffectDuration(args.HitEntities[0], SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(5.0f));
                    var transform = _entityManager.GetComponent<TransformComponent>(args.HitEntities[0]);
                    var randn = _random.Pick(new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 });
                    _transform.SetWorldPosition((args.HitEntities[0], transform), new Vector2(comp.RandomCoordinatesX[randn], comp.RandomCoordinatesY[randn]));
                }
            }
        }
    }

    private void AddPuddleVerb(Entity<SCP106PuddleComponent> entity, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (!ev.CanAccess)
            return;
        if (!TryComp<SCP106Component>(ev.User, out var scp))
            return;
        var user = ev.User;
        var target = ev.Target;
        var text = "";
        if (scp.InDimension == true)
        {
            text = Loc.GetString("scp106-puddletransportexit-verb");
        }
        else
        {
            text = Loc.GetString("scp106-puddletransport-verb");
        }
        AlternativeVerb remverb = new()
        {
            Act = () =>
            {
                if (scp.Puddles.Contains(target))
                {
                    scp.Puddles.Remove(target);
                    QueueDel(target);
                }
            },
            Text = Loc.GetString("scp106-puddleremove-verb"),
            Priority = -1
        };
        AlternativeVerb subverb = new()
        {
            Act = () =>
            {
                var doAfterArgs = new DoAfterArgs(EntityManager,
                user,
                TimeSpan.FromSeconds(scp.Delay),
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
                if(scp.InDimension == true)
                {
                    _popupSystem.PopupEntity(Loc.GetString("scp106-hammaggotson-exit"), target, PopupType.SmallCaution);
                }
            },
            Text = text,
            Priority = -2
        };
        //ev.Verbs.Add(remverb);
        //ev.Verbs.Add(subverb);
    }
    private void OnGhostPuddleDoAfter(Entity<SCP106Component> entity, ref SCP106DoAfterGhostPuddleEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted)
            return;
        if (!TryComp<SCP106Component>(args.User, out var scp))
            return;
        if (!TryComp<DamageableComponent>(args.User, out var dmgable))
            return;
        if (scp.InDimension == false)
        {
            if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
                return;
            var proto = scp.RemoteEntityProto;

            var coords = Transform(args.User).Coordinates;
            var newb = SpawnAtPosition(proto, coords);
            var newscp = EnsureComp<SCP106Component>(newb);
            if (!TryComp<DamageableComponent>(newb, out var newdmgable))
                return;
            _damageable.SetDamage(newb, newdmgable, dmgable.Damage);
            var id = mind.UserId;
            newscp.InDimension = true;
            _actions.AddAction(newb,
                ref newscp.PuddleSpawnActionEntity,
                newscp.PuddleSpawnAction);
            _actions.AddAction(newb,
                ref newscp.PuddleEnterDimensionEntity,
                newscp.PuddleEnterDimensionAction);
            _actions.AddAction(newb,
                ref newscp.PuddleExitPuddleEntity,
                newscp.PuddleExitPuddleAction);
            _actions.RemoveAction(newb, newscp.PuddleEnterPuddleEntity);
            Dirty(newb, newscp);
            _mind.WipeMind(mind.Owner);
            var newMind = _mind.CreateMind(id,
                Comp<MetaDataComponent>(args.User).EntityName);
            _mind.SetUserId(newMind, id);
            _mind.TransferTo(newMind, newb);
            Del(args.User);
        }
        else
        {
            if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
                return;
            var proto = scp.OriginalEntityProto;
            var target = args.Target ?? EntityUid.Invalid;
            var coords = Transform(args.User).Coordinates;
            var newb = SpawnAtPosition(proto, coords);
            var newscp = EnsureComp<SCP106Component>(newb);
            var id = mind.UserId;
            if (!TryComp<DamageableComponent>(newb, out var newdmgable))
                return;
            _damageable.SetDamage(newb, newdmgable, dmgable.Damage);
            newscp.InDimension = false;
            _actions.AddAction(newb,
                ref newscp.PuddleSpawnActionEntity,
                newscp.PuddleSpawnAction);
            Dirty(newb, newscp);
            _mind.WipeMind(mind.Owner);
            var newMind = _mind.CreateMind(id,
                Comp<MetaDataComponent>(args.User).EntityName);

            _mind.SetUserId(newMind, id);
            _mind.TransferTo(newMind, newb);
            Del(args.User);
            _audio.PlayPvs(newscp.ExitSound, newb);
            
        }
    }

    private void OnEnterDimensionAction(SCP106DimensionEnterActionEvent args)
    {
        if (!TryComp<DamageableComponent>(args.Performer, out var dmgable))
            return;
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        if (scp.InDimension == true)
        {
            if (!_mind.TryGetMind(args.Performer, out var mindId, out var mind))
                return;
            var proto = scp.OriginalEntityProto;

            var coords = Transform(args.Performer).Coordinates;
            var newb = SpawnAtPosition(proto, coords);
            var newscp = EnsureComp<SCP106Component>(newb);
            var id = mind.UserId;
            if (!TryComp<DamageableComponent>(newb, out var newdmgable))
                return;
            _damageable.SetDamage(newb, newdmgable, dmgable.Damage);
            newscp.InDimension = false;
            _actions.AddAction(newb,
                ref newscp.PuddleExitDimensionEntity,
                newscp.PuddleExitDimensionAction);
            Dirty(newb, newscp);
            _mind.WipeMind(mind.Owner);
            var newMind = _mind.CreateMind(id,
                Comp<MetaDataComponent>(args.Performer).EntityName);
            newscp.PastPosition = coords;
            newscp.InPocketDimension = true;
            _mind.SetUserId(newMind, id);
            _mind.TransferTo(newMind, newb);
            _actions.RemoveAction(newb, newscp.PuddleSpawnActionEntity);
            _actions.RemoveAction(newb, newscp.PuddleEnterPuddleEntity);
            _actions.RemoveAction(newb, newscp.PuddleDestroyPuddleEntity);
            Del(args.Performer);
            var transform = _entityManager.GetComponent<TransformComponent>(newb);
            newscp.PastMapId = transform.MapID;
            if (_mapSystem.TryGetMap(newscp.PocketMapId, out var mapEnt))
            {
                _transform.SetWorldPosition((newb, transform), new Vector2(0, 0));
                _transform.SetParent(newb, transform, mapEnt.Value);
            }
        }
        args.Handled = true;
    }

    private void OnExitDimensionAction(SCP106DimensionExitActionEvent args)
    {
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        if (!TryComp<DamageableComponent>(args.Performer, out var dmgable))
            return;
        if (scp.InDimension == false)
        {
            if (!_mind.TryGetMind(args.Performer, out var mindId, out var mind))
                return;
            var proto = scp.RemoteEntityProto;

            var coords = Transform(args.Performer).Coordinates;
            var newb = SpawnAtPosition(proto, coords);
            var newscp = EnsureComp<SCP106Component>(newb);
            var oldcords = scp.PastPosition ?? EntityCoordinates.Invalid;
            var oldmap = scp.PastMapId;
            var id = mind.UserId;
            if (!TryComp<DamageableComponent>(newb, out var newdmgable))
                return;
            _damageable.SetDamage(newb, newdmgable, dmgable.Damage);
            newscp.InDimension = true;
            _actions.AddAction(newb,
                ref newscp.PuddleSpawnActionEntity,
                newscp.PuddleSpawnAction);
            _actions.AddAction(newb,
                ref newscp.PuddleEnterDimensionEntity,
                newscp.PuddleEnterDimensionAction);
            _actions.AddAction(newb,
                ref newscp.PuddleExitPuddleEntity,
                newscp.PuddleExitPuddleAction);
            _actions.RemoveAction(newb, newscp.PuddleEnterPuddleEntity);
            Dirty(newb, newscp);
            _mind.WipeMind(mind.Owner);
            var newMind = _mind.CreateMind(id,
                Comp<MetaDataComponent>(args.Performer).EntityName);
            newscp.PastPosition = coords;
            newscp.InPocketDimension = true;
            _mind.SetUserId(newMind, id);
            _mind.TransferTo(newMind, newb);
            Del(args.Performer);
            var transform = _entityManager.GetComponent<TransformComponent>(newb);
            if (_mapSystem.TryGetMap(oldmap, out var mapEnt))
            {
                _transform.SetCoordinates(newb, oldcords);
                _transform.SetParent(newb, transform, mapEnt.Value);
            }
        }
        args.Handled = true;
    }

    private bool IsOnFloor(EntityCoordinates coords)
    {
        var mapCoords = coords.ToMap(EntityManager, _transform);
        var gridUid = _transform.GetGrid(coords) ?? EntityUid.Invalid;
        if (gridUid == EntityUid.Invalid)
        {
            return false;
        }
        var grid = Comp<MapGridComponent>(gridUid);
        var gridEnt = new Entity<MapGridComponent>(gridUid, Comp<MapGridComponent>(gridUid));
        var tile = _mapSystem.GetTileRef(gridEnt, coords);
        if (tile.Tile.IsEmpty)
        {
            return false;
        }
        return true;
    }

    private bool HasOnlyDoorsOrNoWalls(EntityCoordinates coords)
    {
        var mapCoords = coords.ToMap(EntityManager, _transform);
        var gridUid = _transform.GetGrid(coords) ?? EntityUid.Invalid;
        if (gridUid == EntityUid.Invalid)
        {
            return true;
        }
        var grid = Comp<MapGridComponent>(gridUid);
        var gridEnt = new Entity<MapGridComponent>(gridUid, Comp<MapGridComponent>(gridUid));
        var tile = _mapSystem.GetTileRef(gridEnt, mapCoords);
        var anchovies = _mapSystem.GetAnchoredEntities(gridUid, grid, mapCoords);
        bool hasSolidWall = false;
        
        foreach (var anchovy in anchovies)
        {
            if (_tag.HasTag(anchovy, "Wall") ||
                _tag.HasTag(anchovy, "Window"))
            {
                hasSolidWall = true;
            }
        }
        return !hasSolidWall;
    }

    private void OnSkullCollide(EntityUid uid, SCP106SkullComponent component, ref StartCollideEvent args)
    {

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
            while (query.MoveNext(out var pddl))
            {
                allPuddls.Add(pddl.Owner);
            }
            if (allPuddls.Count == 0)
                return;
            var puddl = _random.Pick(allPuddls);
            var ourCoords = Transform(puddl).Coordinates;
            var transform = _entityManager.GetComponent<TransformComponent>(subject);
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
                _statusEffects.TryRemoveStatusEffect(subject, SleepingSystem.StatusEffectForcedSleeping);
                if(TryComp<SleepingComponent>(subject, out var sleeep))
                {
                    _sleep.TryWaking(new Entity<SleepingComponent?>(subject, sleeep), true);
                }
                _audio.PlayPvs(component.ExitSound, puddl);
            }
        }
    }
    private void OnExitPuddleAction(SCP106ExitPuddleActionEvent args)
    {
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        var coords = Transform(args.Performer).Coordinates;
        var user = args.Performer;
        var target = IsTooCloseToPuddles(scp, coords, scp.PuddleExitDistance);
        if (target == EntityUid.Invalid)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-pefar"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        var doAfterArgs = new DoAfterArgs(EntityManager,
        user,
        TimeSpan.FromSeconds(scp.Delay),
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
        if(scp.InDimension == true)
        {
            _popupSystem.PopupEntity(Loc.GetString("scp106-hammaggotson-exit"), target, PopupType.SmallCaution);
        }
    }

    private void OnEnterPuddleAction(SCP106EnterPuddleActionEvent args)
    {
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        var coords = Transform(args.Performer).Coordinates;
        var user = args.Performer;
        var target = IsTooCloseToPuddles(scp, coords, scp.PuddleExitDistance);
        if (target == EntityUid.Invalid)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-pefar"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        var doAfterArgs = new DoAfterArgs(EntityManager,
        user,
        TimeSpan.FromSeconds(scp.Delay),
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
        if(scp.InDimension == true)
        {
            _popupSystem.PopupEntity(Loc.GetString("scp106-hammaggotson-exit"), target, PopupType.SmallCaution);
        }
    }

    private void OnDestroyPuddleAction(SCP106DestroyPuddleActionEvent args)
    {
        if (args.Handled || !TryComp<SCP106Component>(args.Performer, out var scp))
            return;
        var coords = Transform(args.Performer).Coordinates;
        var user = args.Performer;
        var target = IsTooCloseToPuddles(scp, coords, scp.PuddleExitDistance);
        if (target == EntityUid.Invalid)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("scp106-hammaggotson-pefar"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            args.Handled = true;
            return;
        }
        if (scp.Puddles.Contains(target))
        {
            scp.Puddles.Remove(target);
            QueueDel(target);
        }
    }
}