using Content.Shared.Actions;
using Content.Server.Imperial.SCP.SCP106.Components;
using Content.Shared.Imperial.SCP.SCP106.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;
using Robust.Shared.EntitySerialization.Systems;
using Content.Shared.Movement.Pulling.Systems;
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
using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Content.Shared.Chat;
using Content.Server.Chat.Managers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Buckle.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs;
namespace Content.Server.Imperial.SCP.SCP106.Systems;

public sealed partial class SCP106System : EntitySystem
{
    #region Dependencies
    [Dependency] private readonly BuckleSystem _buckleSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatM = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    #endregion
    #region Init and Stuff
    private readonly List<string> BlacklistedTags = ["Wall", "Window"];
    public override void Initialize()
    {
        base.Initialize();
        InitializeSCP106();
        InitializePuddle();
        InitializeSkull();
        InitializeBed();

        SubscribeLocalEvent<SCP106TransmissionPuddleActionEvent>(OnTransmissionPuddleAction);
        SubscribeLocalEvent<SCP106DestroyPuddleActionEvent>(OnDestroyPuddleAction);
        SubscribeLocalEvent<SCP106DimensionSwitchActionEvent>(OnSwitchDimensionAction);
    }
    #endregion

    #region Update
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePuddleStun();
        UpdatePuddleImmunity();
        UpdateDimensionDebuff();
        UpdateBed();
    }
    #endregion

    #region Position checks
    private bool IsTooCloseToWeakling(EntityCoordinates coords, EntityUid scp)
    {
        //It looks up nearby mobs, and if there is one in distance (default: 1 tile) it returns true, otherwise false
        var mapCoords = _transform.ToMapCoordinates(coords);
        var radius = 1f;
        foreach (var mob in _lookup.GetEntitiesInRange(coords, radius, LookupFlags.Dynamic))
        {
            if (!TryComp<MobStateComponent>(mob, out var mobd))
                continue;
            if (mob == scp)
                continue;
            if (mobd.CurrentState == Shared.Mobs.MobState.Dead ||
                mobd.CurrentState == Shared.Mobs.MobState.Critical)
                continue;
            if (!TryComp<TransformComponent>(mob, out var mobTransform))
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
        //It goes through all puddles, calculates distance between them and if one is too close it returns THE PUDDLE UID, otherwise invalid entityuid because i feel like its right (default: 15 tiles)
        var mindis = dis;
        var mindisSquared = mindis * mindis;
        var targetMap = _transform.GetMapId(targetCoords);
        var targetPos = _transform.ToMapCoordinates(targetCoords).Position;

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

    private bool IsOnFloor(EntityCoordinates coords)
    {
        //Returns true if there is a floor underneath us, otherwise FALSE (because we don't want puddles to be in space!!!)
        var mapCoords = _transform.ToMapCoordinates(coords);
        var gridUid = _transform.GetGrid(coords) ?? EntityUid.Invalid;
        if (gridUid == EntityUid.Invalid)
        {
            return false;
        }
        var grid = Comp<MapGridComponent>(gridUid);
        var gridEnt = new Entity<MapGridComponent>(gridUid, grid);
        var tile = _mapSystem.GetTileRef(gridEnt, coords);
        if (tile.Tile.IsEmpty)
        {
            return false;
        }
        return true;
    }

    private bool NoWallsOrWindowsUnderneath(EntityCoordinates coords)
    {
        //So i found out walls and windows have tags, named after them, sooo what basically this method does is
        //It checks entities anchored to the tile underneath us, if there is an entity with a Wall or Window tag it returns false
        var mapCoords = _transform.ToMapCoordinates(coords);
        var gridUid = _transform.GetGrid(coords) ?? EntityUid.Invalid;
        if (gridUid == EntityUid.Invalid)
        {
            return true;
        }
        var grid = Comp<MapGridComponent>(gridUid);
        var gridEnt = new Entity<MapGridComponent>(gridUid, Comp<MapGridComponent>(gridUid));
        var anchovies = _mapSystem.GetAnchoredEntities(gridUid, grid, mapCoords);
        bool hasSolidWall = false;

        foreach (var anchovy in anchovies)
        {
            foreach (string tag in BlacklistedTags)
            {
                if (_tag.HasTag(anchovy, tag))
                {
                    hasSolidWall = true;
                }
            }
        }
        return !hasSolidWall;
    }
    #endregion
    #region Send To Dimension Handle
    private void TeleportEntity(EntityUid hole, EntityUid subject, MapId map, SoundSpecifier globaltpsound, DamageSpecifier damage)
    {
        //We banish a person to the realm, put him to sleep (ig?) and apply a debuff (thats just like bleeding)
        if (!TryComp<TransformComponent>(subject, out var transform))
            return;
        if (_mapSystem.TryGetMap(map, out var mapEnt))
        {
            if (_mind.TryGetMind(subject, out _, out var mindComponent))
            {
                if (_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
                {
                    _chatM.ChatMessageToOne(ChatChannel.Server, Loc.GetString("scp106-hammaggotson-urdamned"), Loc.GetString("chat-manager-server-wrap-message", ("message", Loc.GetString("scp106-hammaggotson-urdamned"))), default, false, session.Channel);
                }
            }
            var query = EntityQueryEnumerator<SCP106Component>();
            var uid = EntityUid.Invalid;
            SCP106Component? scp = null;
            while (query.MoveNext(out var entity, out var comp))
            {
                uid = entity;
                scp = comp;
                break; // we only need one of them so yeah, i am extremely sorry for this
            }
            if (scp != null)
            {
                if (_mind.TryGetMind(uid, out _, out var mindComponentscp))
                {
                    if (_playerManager.TryGetSessionById(mindComponentscp.UserId, out var sessionscp))
                    {
                        var message = Loc.GetString("scp106-hammaggotson-wildhunt", ("name", Comp<MetaDataComponent>(subject).EntityName));
                        _chatM.ChatMessageToOne(ChatChannel.Server, message, Loc.GetString("chat-manager-server-wrap-message", ("message", message)), default, false, sessionscp.Channel);
                    }
                }
            }
            var mapid = transform.MapID;
            ApplyDimensionDebuff(subject, damage, mapid);
            _transform.SetWorldPosition((subject, transform), new Vector2(0, 0));
            _transform.SetParent(subject, transform, mapEnt.Value);
            RemComp<BlockMovementComponent>(subject);
            EnsureComp<PullableComponent>(subject);

        }
        _audio.PlayGlobal(globaltpsound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f)); //i love this
    }
    #endregion
    #region Dimension Debuff Handling
    private void ApplyDimensionDebuff(EntityUid uid, DamageSpecifier damage, MapId mapid)
    {
        //So basically it deals a constant DPS while in dimension, so ppl won't spend their whole life there
        var dimensiondebuff = EnsureComp<SCP106DimensionDebuffComponent>(uid);
        dimensiondebuff.DamagePerSecond = damage;
        dimensiondebuff.PastMapId = mapid;
        dimensiondebuff.NextDamage = _gameTiming.CurTime + TimeSpan.FromSeconds(1.0f);
    }

    private void UpdateDimensionDebuff()
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SCP106DimensionDebuffComponent>();
        while (query.MoveNext(out var entity, out var debuff))
        {
            if (curTime >= debuff.NextDamage)
            {
                _damageable.TryChangeDamage(entity, debuff.DamagePerSecond);
                debuff.NextDamage += TimeSpan.FromSeconds(1.0f);
            }
        }
    }
    #endregion
    #region Transformation
    private EntityUid TransformInto(EntityUid uid, ProtoId<PolymorphPrototype> configuration)
    {
        if (!TryComp<SCP106Component>(uid, out var scp))
            return EntityUid.Invalid;
        if (!TryComp<DamageableComponent>(uid, out var dmgable))
            return EntityUid.Invalid;
        var newb = _polymorph.PolymorphEntity(uid, configuration);
        var newscp = EnsureComp<SCP106Component>(newb ?? EntityUid.Invalid);
        if (!TryComp<DamageableComponent>(newb, out var newdmgable))
            return EntityUid.Invalid;
        _damageable.SetDamage((newb ?? EntityUid.Invalid, newdmgable), dmgable.Damage);
        newscp.Puddles = scp.Puddles;
        newscp.SleepOnAttack = scp.SleepOnAttack;
        newscp.PocketMapId = scp.PocketMapId;
        newscp.InDimension = scp.InDimension;
        newscp.InPocketDimension = scp.InPocketDimension;
        newscp.PastMapId = scp.PastMapId;
        newscp.PastPosition = scp.PastPosition;
        newscp.MaxPuddles = scp.MaxPuddles;
        return newb ?? EntityUid.Invalid;
    }
    #endregion
}
