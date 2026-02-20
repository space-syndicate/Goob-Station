using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Cuffs.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Roles.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Maps;
using Robust.Shared.Physics.Events;
using Content.Shared.Rounding;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Stunnable;
using Content.Shared.Stealth.Components;
using Content.Shared.Stealth;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.DoAfter;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Robust.Shared.Physics.Systems;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Content.Shared.Throwing;
using Content.Shared.Standing;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Imperial.Vampire;

[Virtual]
public partial class SharedVampireSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;


    private void BaseInitialize()
    {
        SubscribeLocalEvent<VampireRecoveryEvent>(OnRecovery);
        SubscribeLocalEvent<VampireSwordEvent>(OnSword);
        SubscribeLocalEvent<VampireNosferatyEvent>(OnNosferaty); // общий
        SubscribeLocalEvent<DamageOnContactComponent, StartCollideEvent>(OnDamageOnContactCollide);

        SubscribeLocalEvent<VampireComponent, MeleeAttackEvent>(OnAttemptMelee);
        SubscribeLocalEvent<VampireComponent, AttackedEvent>(OnDamaged);

        SubscribeLocalEvent<VampireBuffComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<VampireBuffComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<VampireBuffComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);

        SubscribeLocalEvent<VampireComponent, ExaminedEvent>(MaskExamined);

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnVampireStartup);
        SubscribeLocalEvent<VampireComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<VampireComponent, MindRemovedMessage>(OnMindRemoved);
    }

    public override void Initialize()
    {
        base.Initialize();

        BaseInitialize();
        HemomancerInitialize();
        UmbraeInitialize();
        GargantuaInitialize();
    }

    private void OnGetMeleeAttackRate(EntityUid uid, VampireBuffComponent comp, ref GetMeleeAttackRateEvent args)
    {
        args.Rate *= comp.BoostAttackRate;
    }

    private void OnRefreshMovespeed(EntityUid uid, VampireBuffComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.BoostSpeed, comp.BoostSpeed);
    }

    private void OnGetMeleeDamage(EntityUid uid, VampireBuffComponent comp, ref GetMeleeDamageEvent args)
    {
        if (args.Damage.DamageDict.ContainsKey(comp.BuffDamageBluntID))
            args.Damage.DamageDict[comp.BuffDamageBluntID] *= comp.BoostedDamage;

        if (args.Damage.DamageDict.ContainsKey(comp.BuffDamageSlashID))
            args.Damage.DamageDict[comp.BuffDamageSlashID] *= comp.BoostedDamage;
    }

    private void MaskExamined(Entity<VampireComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.TotalDrunk <= 0)
            return;

        if (!_inventory.TryGetSlotEntity(ent, "eyes", out _) && !_inventory.TryGetSlotEntity(ent, "mask", out _))
        {
            var locUser = ("user", Identity.Entity(ent, EntityManager));
            args.PushMarkup(Loc.GetString("vampire-push-markup-eyes", locUser));
        }
    }

    private void OnRecovery(VampireRecoveryEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (_solutionSystem.TryGetInjectableSolution(args.Performer, out var solution, out _))
        {
            // лечим вампира, вводя ему Omnizine
            var toInject = new Solution();
            toInject.AddReagent(args.ReagentID, args.Dose);

            _solutionSystem.TryAddSolution(solution.Value, toInject);
        }

        if (TryComp<StaminaComponent>(args.Performer, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            Dirty(args.Performer, stamina);
        }

        if (TryComp<StatusEffectsComponent>(args.Performer, out var status))
        {
            _statusEffects.TryRemoveStatusEffect(args.Performer, "Stun");
            _statusEffects.TryRemoveStatusEffect(args.Performer, "KnockedDown");
            _statusEffects.TryRemoveStatusEffect(args.Performer, "SlowedDown");
        }

        DealBloodDamage(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnSword(VampireSwordEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.ItemIssued)
        {
            OnIssuingSword(args.Performer);
            vamp.ClawDurationActive = TimeSpan.Zero;

            vamp.CooldownSword = args.CooldownSword;

            // ссылаемся на VampireSwordAction. см VampireBaseAbilities
            _actions.SetCooldown(vamp.GrantedActions[0], vamp.CooldownSword);
            Dirty(args.Performer, vamp);
        }
        else
        {
            OnIssuingSword(args.Performer);

            if (!vamp.VampireTurned)
                vamp.ClawDurationActive = _gameTiming.CurTime + vamp.ClawDuration;
        }
    }

    /// <summary>
    /// добавляет катану в руки
    /// </summary>
    private void OnIssuingSword(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vamp))
            return;

        if (!_net.IsServer)
            return;

        if (!vamp.ItemIssued)
        {
            // если руки не закованы в наручники, то выдаем коготь
            if (TryComp<HandsComponent>(uid, out var hands) &&
                TryComp<CuffableComponent>(uid, out var cuff))
            {
                _audio.PlayPvs(vamp.GetSwordSound, uid);
                var item = Spawn(vamp.SwordId, Transform(uid).Coordinates);

                if (!_hands.CanPickupAnyHand(uid, item, handsComp: hands))
                {
                    _hands.TryDrop(uid);
                }
                // подбираем катану
                if (_hands.TryPickup(uid, item, checkActionBlocker: false, handsComp: hands))
                {
                    vamp.ItemIssued = true;
                    Dirty(uid, vamp);
                }
                else
                {
                    // если не удалось подобрать, удаляем
                    QueueDel(item);
                    _popup.PopupClient(Loc.GetString("vampire-popup-not-hands-free"),
                        uid, uid, PopupType.Medium);
                }
            }
        }
        else
        {
            foreach (var hand in _hands.EnumerateHeld(uid))
            {
                // удаляем катану по мете
                if (MetaData(hand).EntityPrototype?.ID == vamp.SwordId)
                {
                    QueueDel(hand);
                    _audio.PlayPvs(vamp.RemoveSwordSound, uid);
                    vamp.ItemIssued = false;
                    Dirty(uid, vamp);
                    break;
                }
            }
        }
    }

    private void OnNosferaty(VampireNosferatyEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<MeleeWeaponComponent>(args.Performer, out var melee)
        || !TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        // выдаем бафы
        var boost = EnsureComp<VampireBuffComponent>(args.Performer);
        boost.BoostAttackRate = args.AttackRateBoost;
        boost.BoostSpeed = args.BoostSpeed;
        boost.BoostedDamage = args.DamageBoost;
        Dirty(args.Performer, boost);

        _speedSystem.RefreshMovementSpeedModifiers(args.Performer);

        comp.BuffBlocked = true;
        DealBloodDamage(args.Performer, args.CostBlood);
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.NosferatyTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.NosferatyTime, refresh: false, amplitude: 2, frequency: 2);

        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void OnDamageOnContactCollide(Entity<DamageOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        var dmg = new DamageSpecifier
        {
            DamageDict = { [ent.Comp.DamageType] = ent.Comp.Damage }
        };

        _damage.TryChangeDamage(args.OtherEntity, dmg);
    }

    /// <summary>
    /// при попытке атаковать в инвизе - инвиз слетает
    /// </summary>
    private void OnAttemptMelee(Entity<VampireComponent> vamp, ref MeleeAttackEvent args)
    {
        if (!vamp.Comp.InvisibleIsActive)
            return;

        VampireInvisible(vamp);

        // для VampireInvisibleAction
        vamp.Comp.InvisibilityAbilityActive = false;
        Dirty(vamp, vamp.Comp);
    }

    /// <summary>
    /// при получения урона в инвизе - инвиз слетает
    /// </summary>
    private void OnDamaged(Entity<VampireComponent> vamp, ref AttackedEvent args)
    {
        if (!vamp.Comp.InvisibleIsActive)
            return;

        VampireInvisible(vamp);

        // для VampireInvisibleAction
        vamp.Comp.InvisibilityAbilityActive = false;
        Dirty(vamp, vamp.Comp);
    }

    /// <summary>
    /// выдает вампиру невидимость
    /// </summary>
    public void VampireInvisible(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vamp))
            return;

        var stealth = EnsureComp<StealthComponent>(uid);

        if (!vamp.InvisibleIsActive)
        {
            if (vamp.DisguiseIsActive)
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-disguise-on"),
                uid, uid, PopupType.Medium);
                return;
            }

            _stealth.SetVisibility(uid, -2f, stealth);
            _stealth.SetEnabled(uid, true, stealth);

            vamp.DisguiseIsActive = true;
            vamp.InvisibleIsActive = true;

            Dirty(uid, vamp);
            return;
        }

        _stealth.SetVisibility(uid, 1f, stealth);
        _stealth.SetEnabled(uid, false, stealth);

        vamp.InvisibleIsActive = false;
        vamp.DisguiseIsActive = false;
        Dirty(uid, vamp);
    }

    /// <summary>
    /// спавн лужи крови
    /// </summary>
    private void SpawnBloodPuddle(EntityUid uid, string bloodID)
    {
        var coords = Transform(uid).Coordinates;

        if (_net.IsServer)
        {
            var puddle = Spawn(bloodID, coords);

            if (_solutionSystem.TryGetSolution(puddle, "puddle", out var solution))
            {
                var bloodSolution = new Solution();
                bloodSolution.AddReagent("Blood", 10f);

                _solutionSystem.TryAddSolution(solution.Value, bloodSolution);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        BaseUpdate();
        HemomancerUpdate();
        UmbraeUpdate();
        GargantuaUpdate();
    }

    private void BaseUpdate()
    {
        var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.BuffBlocked)
                continue;

            if (comp.BuffBlocked && _gameTiming.CurTime >= comp.BuffBlockedUntil)
            {
                // т.к query распространяется и на RushBlood
                RemComp<VampireBuffComponent>(uid);
                _speedSystem.RefreshMovementSpeedModifiers(uid);
                comp.BuffBlocked = false;
                Dirty(uid, comp);
            }
        }

        var ghoulQuery = EntityQueryEnumerator<GhoulComponent>();
        while (ghoulQuery.MoveNext(out var uid, out var comp))
        {
            // заставляем упырей пить кровь
            if (comp.NextBloodDecay == TimeSpan.Zero)
            {
                comp.NextBloodDecay = _gameTiming.CurTime + comp.BloodDecayInterval;
                Dirty(uid, comp);
            }

            if (_gameTiming.CurTime >= comp.NextBloodDecay)
            {
                // наносим урон каждые BloodDecayInterval секунд
                DealGhoulBloodDamage(uid, comp.BloodDecayAmount, comp);
                comp.NextBloodDecay = _gameTiming.CurTime + comp.BloodDecayInterval;
                Dirty(uid, comp);

                // если урон больше количества крови, то применяем дебафы
                if (comp.BloodDamage >= comp.CritThreshold)
                {
                    if (TryComp<StaminaComponent>(uid, out var stamina))
                    {
                        var dmg = new DamageSpecifier();
                        dmg.DamageDict["Bloodloss"] = FixedPoint2.New(30);

                        _damage.TryChangeDamage(uid, dmg);
                        SpawnBloodPuddle(uid, comp.GhoulPuddleID);
                        _stamina.TakeStaminaDamage(uid, 70f, stamina);

                        if (_net.IsServer)
                            _jitterSystem.DoJitter(uid, comp.ShakingTime, refresh: false, amplitude: 15f, frequency: 4f);
                    }
                }
            }
        }

        var vampClaw = EntityQueryEnumerator<VampireComponent>();
        while (vampClaw.MoveNext(out var uid, out var vamp))
        {
            if (!_net.IsServer)
                continue;

            if (_gameTiming.CurTime >= vamp.ClawDurationActive && vamp.ItemIssued && vamp.ClawDurationActive != TimeSpan.Zero)
            {
                OnIssuingSword(uid);

                // ссылаемся на VampireSwordAction. см VampireBaseAbilities
                _actions.SetCooldown(vamp.GrantedActions[0], vamp.CooldownSword);
                vamp.ClawDurationActive = TimeSpan.Zero;
                Dirty(uid, vamp);
            }
        }
    }
    public void DealGhoulBloodDamage(EntityUid uid, float damage, GhoulComponent component)
    {
        component.BloodDamage = MathF.Min(component.BloodDamage + damage, component.CritThreshold);
        Dirty(uid, component);
        SetGhoulBloodAlert(uid, component);
    }

    public void SetGhoulBloodAlert(EntityUid uid, GhoulComponent component)
    {
        if (!HasComp<GhoulComponent>(uid))
        {
            _alerts.ClearAlert(uid, component.BloodAlert);
            return;
        }

        // вычисляем, какой должен быть спрайт в зависимости от количества крови у упыря
        var severity = ContentHelpers.RoundToLevels(
            MathF.Max(0f, component.CritThreshold - component.BloodDamage), component.CritThreshold, component.NumberBloodSections);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
    }

    public void SetBloodAlert(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества крови у вампира
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.BloodDamage),
        component.CritThreshold, component.NumberBloodSections);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
    }

    public void SetBloodCounterAlert(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества выпитой крови вампиром
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.TotalDrunk), component.MaxDrink, component.NumberSections);
        _alerts.ShowAlert(uid, component.BloodCounterAlert, (short)severity);
    }

    public void DealBloodDamage(EntityUid uid, float damage, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.BloodDamage = MathF.Min(component.BloodDamage + damage, component.CritThreshold);
        Dirty(uid, component);
        SetBloodAlert(uid, component);
    }

    private void OnVampireStartup(Entity<VampireComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.SelectingSubgroupActionEntity == null)
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.SelectingSubgroupActionEntity, ent.Comp.SelectingSubgroupAction);
            Dirty(ent.Owner, ent.Comp);
        }

        // добавляем рацию
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ent.Owner);
        transmitter.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        transmitter.Channels.Add(new ProtoId<RadioChannelPrototype>(ent.Comp.VampireRadioID));

        var activeRadio = EnsureComp<ActiveRadioComponent>(ent.Owner);
        activeRadio.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        activeRadio.Channels.Add(new ProtoId<RadioChannelPrototype>(ent.Comp.VampireRadioID));

        EnsureComp<IntrinsicRadioReceiverComponent>(ent.Owner);

        // для OnJerk
        if (!HasComp<VampireJerkComponent>(ent))
            AddComp<VampireJerkComponent>(ent);

        if (!_prototypeManager.TryIndex<VampireAbilityListPrototype>(ent.Comp.VampireAbilitiesID[VampireAbilityType.Base], out var baseAbilities))
            return;

        // выдача базовых способностей
        if (ent.Comp.GrantedActions.Count == 0)
        {
            foreach (var proto in baseAbilities.Abilities)
            {
                EntityUid? actionEnt = null;
                _actions.AddAction(ent, ref actionEnt, proto);

                if (actionEnt != null)
                    ent.Comp.GrantedActions.Add(actionEnt.Value);
            }
        }

        SetBloodAlert(ent.Owner, ent.Comp);
        SetBloodCounterAlert(ent.Owner, ent.Comp);
    }

    private void OnMindAdded(Entity<VampireComponent> ent, ref MindAddedMessage args)
    {
        _roleSystem.MindAddRole(args.Mind, ent.Comp.MindRoleVampireID, mind: args.Mind.Comp);
    }

    private void OnMindRemoved(Entity<VampireComponent> ent, ref MindRemovedMessage args)
    {
        _roleSystem.MindRemoveRole<VampireRoleComponent>((args.Mind.Owner, args.Mind.Comp));
    }
}
