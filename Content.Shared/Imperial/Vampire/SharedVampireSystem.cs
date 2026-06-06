using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.EntitySystems;
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
using Content.Shared.StatusEffect;
using System.Runtime.CompilerServices;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using System.Linq;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Shared.Imperial.Vampire;

[Virtual]
public partial class SharedVampireSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StatusEffectNew.StatusEffectsSystem _statusEffects = default!;
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
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;


    private void BaseInitialize()
    {
        SubscribeLocalEvent<VampireRecoveryEvent>(OnRecovery);
        SubscribeLocalEvent<VampireRecoveryGhoulEvent>(OnRecoveryGhoul);
        SubscribeLocalEvent<VampireSwordEvent>(OnSword);
        SubscribeLocalEvent<VampireSwordPlusEvent>(OnSwordPlus);
        SubscribeLocalEvent<VampireNosferatyEvent>(OnNosferaty); // общий
        SubscribeLocalEvent<DamageOnContactComponent, StartCollideEvent>(OnDamageOnContactCollide);

        SubscribeLocalEvent<AbilityComponent, MeleeAttackEvent>(OnAttemptMelee);
        SubscribeLocalEvent<AbilityComponent, AttackedEvent>(OnDamaged);

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


    public bool HasEnoughBloodShared(EntityUid entity, float costBlood)
    {
        if (TryComp<VampireComponent>(entity, out var vamp))
            return vamp.BloodDamage + costBlood < vamp.CritThreshold;

        if (TryComp<GhoulComponent>(entity, out var ghoul))
            return ghoul.BloodDamage + costBlood < ghoul.CritThreshold;

        return false;
    }

    public void DealAbilityBloodDamageShared(EntityUid entity, float damage)
    {
        if (TryComp<VampireComponent>(entity, out var vamp))
        {
            DealBloodDamage(entity, damage);
        }
        else if (TryComp<GhoulComponent>(entity, out var ghoul))
        {
            DealGhoulBloodDamage(entity, damage, ghoul);
        }
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
        if (args.Damage.DamageDict.ContainsKey(comp.BuffDamageID[0]))
            args.Damage.DamageDict[comp.BuffDamageID[0]] *= comp.BoostedDamage;

        if (args.Damage.DamageDict.ContainsKey(comp.BuffDamageID[1]))
            args.Damage.DamageDict[comp.BuffDamageID[1]] *= comp.BoostedDamage;
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
        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
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

        DealAbilityBloodDamageShared(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnRecoveryGhoul(VampireRecoveryGhoulEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var userPos = Transform(args.Performer).Coordinates;
        var targetPos = args.Target;

        if (!userPos.TryDistance(EntityManager, targetPos, out var distance) || distance > args.Radius)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-far-away"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var target = _lookup.GetEntitiesInRange(args.Target, 0.1f, LookupFlags.Uncontained | LookupFlags.Dynamic)
        .FirstOrDefault(el => el != args.Performer && HasComp<MobStateComponent>(el));

        if (!HasComp<GhoulComponent>(target)) return;

        if (_solutionSystem.TryGetInjectableSolution(target, out var solution, out _))
        {
            // лечим упыря, вводя ему Omnizine
            var toInject = new Solution();
            toInject.AddReagent(args.ReagentID, args.Dose);

            _solutionSystem.TryAddSolution(solution.Value, toInject);
        }

        if (TryComp<StaminaComponent>(target, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            Dirty(target, stamina);
        }

        if (TryComp<StatusEffectsComponent>(target, out var status))
        {
            _statusEffects.TryRemoveStatusEffect(target, "Stun");
            _statusEffects.TryRemoveStatusEffect(target, "KnockedDown");
            _statusEffects.TryRemoveStatusEffect(target, "SlowedDown");
        }

        DealAbilityBloodDamageShared(args.Performer, args.CostBlood);
        args.Handled = true;
    }

    private void OnSword(VampireSwordEvent args)
    {
        if (!_net.IsServer)
            return;

        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (vamp.ItemIssued)
        {
            OnIssuingSword(args.Performer);
            vamp.ClawDurationActive = TimeSpan.Zero;

            vamp.CooldownSword = args.CooldownSword;

            // ссылаемся на VampireSwordAction. см VampireBaseAbilities
            if (TryComp<VampireComponent>(args.Performer, out var vampire)) _actions.SetCooldown(vampire.GrantedActions[0], vamp.CooldownSword);
            else if (TryComp<GhoulComponent>(args.Performer, out var ghoul)) _actions.SetCooldown(_entityManager.GetEntity(ghoul.GhoulVampireSwordAction), vamp.CooldownSword);
            else return;
            Dirty(args.Performer, vamp);
        }
        else
        {
            OnIssuingSword(args.Performer);
            vamp.ClawDurationActive = _gameTiming.CurTime + vamp.ClawDuration;
            Dirty(args.Performer, vamp);
        }
    }

    private void OnSwordPlus(VampireSwordPlusEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var vampire) && !TryComp<GhoulComponent>(args.Performer, out var ghoul)) return;
        var vamp = EnsureComp<AbilityComponent>(args.Performer);

        if (vamp.ItemIssued)
        {
            OnIssuingSword(args.Performer);
            vamp.ClawDurationActive = TimeSpan.Zero;
            vamp.CooldownSword = args.CooldownSword;
            Dirty(args.Performer, vamp);
        }
        else
        {
            OnIssuingSword(args.Performer);
            Dirty(args.Performer, vamp);
        }
    }

    /// <summary>
    /// добавляет катану в руки
    /// </summary>
    public void OnIssuingSword(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vampire) && !TryComp<GhoulComponent>(uid, out var ghoul))
            return;

        var vamp = EnsureComp<AbilityComponent>(uid);

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
                if (MetaData(hand).EntityPrototype!.ID == vamp.SwordId)
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
        if (!TryComp<VampireComponent>(args.Performer, out var vampire) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        var comp = EnsureComp<AbilityComponent>(args.Performer);

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
        DealAbilityBloodDamageShared(args.Performer, args.CostBlood);
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

        var dmg = new DamageSpecifier(ent.Comp.Damage);

        _damage.TryChangeDamage(args.OtherEntity, dmg);
    }

    /// <summary>
    /// при попытке атаковать в инвизе - инвиз слетает
    /// </summary>
    private void OnAttemptMelee(Entity<AbilityComponent> comp, ref MeleeAttackEvent args)
    {
        if (!TryComp<VampireComponent>(comp, out var vamp) && !TryComp<GhoulComponent>(comp, out var ghoul))
            return;

        if (!comp.Comp.InvisibleIsActive)
            return;

        VampireInvisible(comp);

        // для VampireInvisibleAction
        comp.Comp.InvisibilityAbilityActive = false;
        Dirty(comp, comp.Comp);
    }

    /// <summary>
    /// при получения урона в инвизе - инвиз слетает
    /// </summary>
    private void OnDamaged(Entity<AbilityComponent> comp, ref AttackedEvent args)
    {
        if (!TryComp<VampireComponent>(comp, out var vamp) && !TryComp<GhoulComponent>(comp, out var ghoul))
            return;

        if (!comp.Comp.InvisibleIsActive)
            return;

        VampireInvisible(comp);

        // для VampireInvisibleAction
        comp.Comp.InvisibilityAbilityActive = false;
        Dirty(comp, comp.Comp);
    }

    /// <summary>
    /// выдает вампиру невидимость
    /// </summary>
    public void VampireInvisible(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vampire) && !TryComp<GhoulComponent>(uid, out var ghoul)) return;
        if (!_net.IsServer) return;

        var stealth = EnsureComp<StealthComponent>(uid);
        var comp = EnsureComp<AbilityComponent>(uid);

        if (!comp.InvisibleIsActive)
        {
            if (comp.DisguiseIsActive)
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-disguise-on"),
                uid, uid, PopupType.Medium);
                return;
            }

            if (comp.HaloUid != null) QueueDel(comp.HaloUid);

            _stealth.SetVisibility(uid, -2f, stealth);
            _stealth.SetEnabled(uid, true, stealth);

            comp.DisguiseIsActive = true;
            comp.InvisibleIsActive = true;

            Dirty(uid, comp);
            return;
        }

        comp.HaloUid = Spawn(comp.HaloEffect, Transform(uid).Coordinates);
        _transform.SetParent(comp.HaloUid.Value, uid);

        _stealth.SetVisibility(uid, 1f, stealth);
        _stealth.SetEnabled(uid, false, stealth);

        comp.InvisibleIsActive = false;
        comp.DisguiseIsActive = false;
        Dirty(uid, comp);
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
                var bloodReagent = "Blood";
                bloodSolution.AddReagent(bloodReagent, 10f);

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
        var query = EntityQueryEnumerator<AbilityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<VampireComponent>(uid, out var vampire) && !TryComp<GhoulComponent>(uid, out var ghoul))
                continue;

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

        var vampirelQuery = EntityQueryEnumerator<AbilityComponent>();
        while (vampirelQuery.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<VampireComponent>(uid, out var vampire))
                continue;

            if (_mobStateSystem.IsDead(uid))
                continue;

            if (comp.NextBloodDecay == TimeSpan.Zero)
            {
                comp.NextBloodDecay = _gameTiming.CurTime + comp.BloodDecayInterval;
                Dirty(uid, comp);
            }

            if (_gameTiming.CurTime >= comp.NextBloodDecay)
            {
                // наносим урон каждые BloodDecayInterval секунд
                DealAbilityBloodDamageShared(uid, comp.BloodDecayAmount);
                comp.NextBloodDecay = _gameTiming.CurTime + comp.BloodDecayInterval;
                Dirty(uid, comp);

                // если урон больше количества крови, то применяем дебафы
                if (vampire.BloodDamage >= vampire.CritThreshold)
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

        var ghoulQuery = EntityQueryEnumerator<GhoulComponent>();
        while (ghoulQuery.MoveNext(out var uid, out var comp))
        {
            if (_mobStateSystem.IsDead(uid))
                continue;

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

        var vampClaw = EntityQueryEnumerator<AbilityComponent>();
        while (vampClaw.MoveNext(out var uid, out var vamp))
        {
            if (!_net.IsServer)
                continue;

            if (_gameTiming.CurTime >= vamp.ClawDurationActive && vamp.ItemIssued && vamp.ClawDurationActive != TimeSpan.Zero)
            {
                OnIssuingSword(uid);

                // ссылаемся на VampireSwordAction. см VampireBaseAbilities
                if (TryComp<VampireComponent>(uid, out var vampire)) _actions.SetCooldown(vampire.GrantedActions[0], vamp.CooldownSword);
                else if (TryComp<GhoulComponent>(uid, out var ghoul)) _actions.SetCooldown(_entityManager.GetEntity(ghoul.GhoulVampireSwordAction), vamp.CooldownSword);
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
        var comp = EnsureComp<AbilityComponent>(uid);
        if (!HasComp<VampireComponent>(uid))
        {
            _alerts.ClearAlert(uid, comp.BloodAlert);
            return;
        }

        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества крови у вампира
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.CritThreshold - component.BloodDamage),
        component.CritThreshold, comp.NumberBloodSections);
        _alerts.ShowAlert(uid, comp.BloodAlert, (short)severity);
    }

    public void SetBloodCounterAlert(EntityUid uid, VampireComponent? component = null)
    {
        var comp = EnsureComp<AbilityComponent>(uid);
        if (!HasComp<VampireComponent>(uid))
        {
            _alerts.ClearAlert(uid, comp.BloodCounterAlert);
            return;
        }

        if (!Resolve(uid, ref component, false) || component.Deleted)
            return;

        // вычисляем, какой должен быть спрайт в зависимости от количества выпитой крови вампиром
        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, component.TotalDrunk), comp.MaxDrink, comp.NumberSections);
        _alerts.ShowAlert(uid, comp.BloodCounterAlert, (short)severity);
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
        var comp = EnsureComp<AbilityComponent>(ent);
        if (ent.Comp.SelectingSubgroupActionEntity == null)
        {
            _actions.AddAction(ent.Owner, ref ent.Comp.SelectingSubgroupActionEntity, comp.SelectingSubgroupAction);
            Dirty(ent.Owner, ent.Comp);
        }

        // добавляем рацию
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ent.Owner);
        transmitter.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        transmitter.Channels.Add(new ProtoId<RadioChannelPrototype>(comp.VampireRadioID));

        var activeRadio = EnsureComp<ActiveRadioComponent>(ent.Owner);
        activeRadio.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        activeRadio.Channels.Add(new ProtoId<RadioChannelPrototype>(comp.VampireRadioID));

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
        var comp = EnsureComp<AbilityComponent>(ent);
        _roleSystem.MindAddRole(args.Mind, comp.MindRoleVampireID, mind: args.Mind.Comp);
    }

    private void OnMindRemoved(Entity<VampireComponent> ent, ref MindRemovedMessage args)
    {
        RaiseNetworkEvent(new VampireMindRemovedEvent(_entityManager.GetNetEntity(ent)));
    }
}
