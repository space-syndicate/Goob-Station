using Content.Shared._BlackM.Headcrab;
using Content.Goobstation.Shared.Overlays;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Content.Shared.Rejuvenate;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Content.Server.Chat.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.Headcrab;

public sealed class HeadcrabSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier LeapSound =
        new SoundPathSpecifier("/Audio/_BlackM/headcrab/leap.ogg");

    private static readonly SoundSpecifier ScreamSound =
        new SoundPathSpecifier("/Audio/_BlackM/headcrab/scream.ogg");

    private static readonly int HeadcrabSoundCount = 6;
    private static readonly ProtoId<NpcFactionPrototype> XenFaction = "BlackMXen";

    private readonly HashSet<EntityUid> _recentlyHit = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadcrabComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HeadcrabComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HeadcrabComponent, HeadcrabLeapEvent>(OnLeap);
        SubscribeLocalEvent<HeadcrabComponent, HeadcrabGrabEvent>(OnGrab);
        SubscribeLocalEvent<HeadcrabComponent, HeadcrabAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<HeadcrabComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<HeadcrabComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<HeadcrabCapturedComponent, EntitySpokeEvent>(OnCapturedSpeak);
        SubscribeLocalEvent<HeadcrabCapturedComponent, ComponentStartup>(OnCapturedStartup);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
    }

    private void OnInit(EntityUid uid, HeadcrabComponent comp, ComponentInit args)
    {
        comp.LeapActionUid = _actions.AddAction(uid, comp.LeapAction);
        comp.GrabActionUid = _actions.AddAction(uid, comp.GrabAction);

        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid);
    }

    private void OnMobStateChanged(EntityUid uid, HeadcrabComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        RemComp<HeadcrabLeapingComponent>(uid);
        _audio.PlayPvs(comp.DeathSound, uid);
    }

    private void OnShutdown(EntityUid uid, HeadcrabComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(comp.LeapActionUid);
        _actions.RemoveAction(comp.GrabActionUid);
        _recentlyHit.Remove(uid);
    }

    private void OnLeap(EntityUid uid, HeadcrabComponent comp, HeadcrabLeapEvent args)
    {
        if (args.Handled)
            return;

        if (_mobState.IsDead(uid))
            return;

        if (TryComp<HeadcrabLeapingComponent>(uid, out var current) && _timing.CurTime < current.EndTime)
            return;

        var targetPos = _transform.ToMapCoordinates(args.Target);
        var selfPos = _transform.GetMapCoordinates(uid);

        if (targetPos.MapId != selfPos.MapId)
            return;

        var diff = targetPos.Position - selfPos.Position;
        var dist = diff.Length();

        if (dist < 0.1f)
            return;

        args.Handled = true;

        _recentlyHit.Remove(uid);

        var leapDist = Math.Clamp(dist + 0.5f, 1f, args.Distance);
        var vec = diff / dist * leapDist;

        var flightTime = leapDist / args.Speed;
        var leaping = EnsureComp<HeadcrabLeapingComponent>(uid);
        leaping.StaminaDamage = args.StaminaDamage;
        leaping.EndTime = _timing.CurTime + TimeSpan.FromSeconds(flightTime + 0.15f);

        _throwing.TryThrow(uid, vec, args.Speed, animated: true, doSpin: false);

        _audio.PlayPvs(LeapSound, uid);
    }

    private void OnCollide(EntityUid uid, HeadcrabComponent comp, ref StartCollideEvent args)
    {
        if (!TryComp<HeadcrabLeapingComponent>(uid, out var leaping))
            return;

        if (_timing.CurTime > leaping.EndTime)
        {
            RemCompDeferred<HeadcrabLeapingComponent>(uid);
            return;
        }

        var target = args.OtherEntity;

        if (target == uid)
            return;
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;
        if (HasComp<HeadcrabCapturedComponent>(target))
            return;
        if (!TryComp<StaminaComponent>(target, out _))
            return;

        if (_recentlyHit.Contains(uid))
            return;
        _recentlyHit.Add(uid);

        _stamina.TakeStaminaDamage(target, leaping.StaminaDamage, source: uid, immediate: false);

        comp.LeapHits.TryGetValue(target, out var hits);
        comp.LeapHits[target] = hits + 1;

        if (comp.LeapHits[target] >= comp.LeapsToKnockdown)
        {
            _stamina.TakeStaminaDamage(target, 200f, source: uid, immediate: true);
            comp.LeapHits[target] = 0;
            _popup.PopupEntity(Loc.GetString("headcrab-leap-knockdown"), target);
        }

        RemCompDeferred<HeadcrabLeapingComponent>(uid);
    }

    private void OnGrab(EntityUid uid, HeadcrabComponent comp, HeadcrabGrabEvent args)
    {
        if (args.Handled)
            return;

        var targetPos = _transform.ToMapCoordinates(args.Target);
        EntityUid? targetEnt = null;

        foreach (var ent in _lookup.GetEntitiesInRange(targetPos, 1.5f))
        {
            if (ent == uid)
                continue;
            if (!HasComp<HumanoidAppearanceComponent>(ent))
                continue;
            if (HasComp<HeadcrabCapturedComponent>(ent))
                continue;
            if (!TryComp<MobStateComponent>(ent, out var mobState))
                continue;

            var isCrit = mobState.CurrentState == MobState.Critical;
            var isStamCrit = TryComp<StaminaComponent>(ent, out var stam) &&
                             stam.StaminaDamage >= stam.CritThreshold;

            if (!isCrit && !isStamCrit)
                continue;

            targetEnt = ent;
            break;
        }

        if (targetEnt == null)
        {
            _popup.PopupEntity(Loc.GetString("headcrab-grab-fail"), uid, uid);
            return;
        }

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, 2f, new HeadcrabAttachDoAfterEvent(), uid, targetEnt.Value)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("headcrab-grab-start"), targetEnt.Value);
    }

    private void OnAttachDoAfter(EntityUid uid, HeadcrabComponent comp, HeadcrabAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        if (_inventory.TryGetSlotEntity(target, "head", out _))
            _inventory.TryUnequip(target, "head", force: true, silent: true);

        var helmet = Spawn("ClothingHeadHeadcrab", Transform(target).Coordinates);
        _inventory.TryEquip(target, helmet, "head", force: true, silent: true);

        EnsureComp<HeadcrabCapturedComponent>(target);

        var rejuvenate = new RejuvenateEvent();
        RaiseLocalEvent(target, rejuvenate);

        _mind.TransferTo(mindId, target, mind: mind);

        QueueDel(uid);
    }

    private void OnCapturedStartup(EntityUid uid, HeadcrabCapturedComponent comp, ComponentStartup args)
    {
        var vision = EnsureComp<NightVisionComponent>(uid);
        vision.Color = Color.FromHex("#808080");
        vision.ActivateSound = null;
        vision.DeactivateSound = null;
        Dirty(uid, vision);

        _factionSystem.ClearFactions(uid, dirty: false);
        _factionSystem.AddFaction(uid, XenFaction);
    }

    private void OnCapturedSpeak(EntityUid uid, HeadcrabCapturedComponent comp, EntitySpokeEvent args)
    {
        _audio.PlayPvs(ScreamSound, uid);
    }

    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!HasComp<HeadcrabCapturedComponent>(args.Sender))
            return;

        var random = new System.Random();
        var index = random.Next(1, HeadcrabSoundCount + 1);
        args.Message = Loc.GetString($"headcrab-sound-{index}");
    }
}
