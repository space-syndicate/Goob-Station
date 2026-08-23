using Content.Shared.Verbs;
using Content.Shared.Imperial.Vampire;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Content.Server.Chat.Managers;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;
using Content.Shared.Roles;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Body.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Cloning;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Content.Server.EUI;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Robust.Server.Audio;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Server.Bible.Components;
using Content.Shared.Alert;
using Content.Server.Body;
using System.Linq;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Shared.Prayer;
using Content.Shared.Roles.Components;

namespace Content.Server.Imperial.Vampire;


public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedVampireSystem _vampireSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;
    [Dependency] private readonly VisualBodySystem _visualBodySystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    private void VampireInitialize()
    {
        SubscribeLocalEvent<VampireComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbsCombined);
        SubscribeLocalEvent<GhoulComponent, GetVerbsEvent<InnateVerb>>(OnGetDrinkingGhoul);
        SubscribeLocalEvent<GhoulComponent, InteractUsingEvent>(OnCureGhoulStart);
        SubscribeLocalEvent<VampireComponent, InteractUsingEvent>(OnCureVampireStart);
        SubscribeLocalEvent<GhoulComponent, VampireCureGhoulDoAfterEvent>(OnCureGhoul);
        SubscribeLocalEvent<VampireComponent, VampireCureDoAfterEvent>(OnCureVampire);
        SubscribeLocalEvent<VampireComponent, VampireEnvelopeDoAfterEvent>(OnEnvelopeCompleteVampire);
        SubscribeLocalEvent<GhoulComponent, VampireDrinkingDoAfterEvent>(OnDrinkingCompleteGhoul);
        SubscribeLocalEvent<VampireComponent, VampireDrinkingDoAfterEvent>(OnDrinkingCompleteVampire);

        SubscribeNetworkEvent<VampireMindRemovedEvent>(OnMindRemoved);
        SubscribeLocalEvent<VampireComponent, MobStateChangedEvent>(OnDead);
    }

    public override void Initialize()
    {
        base.Initialize();

        VampireInitialize();
        VampireAbilitiesInitialize();
    }

    private void OnGetVerbsCombined(EntityUid uid, VampireComponent vamp, GetVerbsEvent<InnateVerb> args)
    {
        var comp = EnsureComp<AbilityComponent>(uid);
        if (!args.CanAccess || !args.CanInteract || comp.InvisibleIsActive || args.Target == uid
            || !_mobState.IsAlive(args.Target))
            return;

        // верб для превращения цели в упыря
        if (!HasComp<GhoulComponent>(args.Target) && !HasComp<VampireComponent>(args.Target) && HasComp<MindContainerComponent>(args.Target)
            && !HasComp<MindShieldComponent>(args.Target) && !_statusEffects.HasStatusEffect(uid, vamp.CooldownStatusEffectAppealGhouls)
            && HasComp<ActorComponent>(args.Target) && HasComp<BloodstreamComponent>(args.Target))
        {
            var verbConvert = new InnateVerb
            {
                Act = () => StartConversion(args.User, args.Target),
                Text = Loc.GetString("vampire-verb-envelope-ghoul-text"),
                Message = Loc.GetString("vampire-verb-envelope-ghoul-message"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Stellark/Vampire/verbs/appeal.png")),
                Priority = 0
            };
            args.Verbs.Add(verbConvert);
        }

        // верб для питья крови
        if (!HasComp<GhoulComponent>(args.Target) && !HasComp<VampireComponent>(args.Target) && HasComp<BloodstreamComponent>(args.Target))
        {
            var verbDrinkBlood = new InnateVerb
            {
                Act = () => StartDrinking(args.User, args.Target),
                Text = Loc.GetString("vampire-drinking-envelope-text"),
                Message = Loc.GetString("vampire-drinking-envelope-message"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Stellark/Vampire/verbs/drinkBlood.png")),
                Priority = 1
            };
            args.Verbs.Add(verbDrinkBlood);
        }
    }

    private void OnCureVampireStart(EntityUid uid, VampireComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PrayableComponent>(args.Used, out var prayable))
            return;

        if (prayable.BibleUserOnly && !TryComp<BibleUserComponent>(args.User, out _))
            return;

        if (TryComp<VampireComponent>(uid, out var vamp) && TryComp<AbilityComponent>(uid, out var abilityComponent))
        {
            if (abilityComponent.VampireTurned)
            {
                _popup.PopupEntity(Loc.GetString("vampire-popup-vampire-turned"),
                args.User, args.User, PopupType.Medium);

                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("vampire-popup-ghoul-rite"),
            args.User, args.User, PopupType.Medium);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.VampireCure,
            new VampireCureDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnCureVampire(Entity<VampireComponent> ent, ref VampireCureDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null) return;

        VampireMindRemoved(ent);
    }

    private void StartDrinking(EntityUid drinker, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, drinker, TimeSpan.FromSeconds(2f),
        new VampireDrinkingDoAfterEvent(), drinker, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void StartConversion(EntityUid vampire, EntityUid target)
    {
        if (!TryComp<VampireComponent>(vampire, out var vamp))
            return;

        if (vamp.GhoulQuantity == vamp.MaxNumberGhouls)
        {
            _popup.PopupEntity(Loc.GetString("vampire-popup-max-number-ghouls"), vampire, vampire, PopupType.Medium);
            return;
        }

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-vampire-transform",
            ("target", MetaData(target).EntityName)),
            vampire, vampire, PopupType.Medium);

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-ghoul-transform"),
            target, target, PopupType.Medium);

        var doAfterArgs = new DoAfterArgs(EntityManager, vampire, vamp.ConversionGhoulTime,
            new VampireEnvelopeDoAfterEvent(), vampire, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// обработчик DoAfter
    /// </summary>
    private void OnEnvelopeCompleteVampire(EntityUid uid, VampireComponent comp, ref VampireEnvelopeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target
        || TryComp<GhoulComponent>(target, out var ghoulComp))
            return;

        ConvertToGhoul(args.User, target);
        args.Handled = true;
    }

    /// <summary>
    /// обработчик DoAfter для вампира
    /// </summary>
    private void OnDrinkingCompleteVampire(Entity<VampireComponent> ent, ref VampireDrinkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        DrinkingComplete(args.User, target, ent.Comp.BloodPerTick);
        args.Handled = true;
    }

    private void DrinkingComplete(EntityUid drinker, EntityUid target, float amount)
    {
        // проверяем, есть ли кровь у цели
        if (!TryComp<BloodstreamComponent>(target, out var blood) || blood.BloodSolution == null ||
            !TryComp<SolutionComponent>(blood.BloodSolution.Value.Owner, out var solutionComp))
        {
            _popup.PopupEntity(Loc.GetString("vampire-drinking-no-blood"), drinker, drinker, PopupType.Medium);
            return;
        }

        var solution = solutionComp.Solution;

        if (solution.Volume.Float() < 20)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drinking-no-blood"), drinker, drinker, PopupType.Medium);
            return;
        }

        TryComp<VampireComponent>(drinker, out var vamp);
        TryComp<GhoulComponent>(drinker, out var ghoul);

        var damage = new DamageSpecifier();

        if (vamp == null && ghoul == null)
            return;

        // вычисляем текущее количество крови
        float currentBlood = vamp != null ? vamp.CritThreshold - vamp.BloodDamage : ghoul!.CritThreshold - ghoul.BloodDamage;

        if (currentBlood >= 100 || !HasComp<MindContainerComponent>(target) || !HasComp<ActorComponent>(target))
        {
            if (vamp != null)
            {
                // мы просто засчитываем эту кровь в TotalDrunk, но BloodDamage не понижаем
                if (currentBlood >= 100 && HasComp<MindContainerComponent>(target) && HasComp<ActorComponent>(target))
                {
                    vamp.TotalDrunk += amount;
                    _vampireSystem.SetBloodCounterAlert(drinker, vamp);
                }
                // мы просто понижаем BloodDamage, но в TotalDrunk не засчитываем
                else if (!HasComp<MindContainerComponent>(target) || !HasComp<ActorComponent>(target))
                {
                    if (currentBlood >= 100)
                    {
                        _popup.PopupEntity(Loc.GetString("vampire-popup-target-no-mind-full"), drinker, drinker, PopupType.Medium);
                        return;
                    }

                    vamp.BloodDamage = Math.Max(vamp.BloodDamage - amount / 2, 0);
                    _vampireSystem.SetBloodAlert(drinker, vamp);
                    _popup.PopupEntity(Loc.GetString("vampire-popup-target-no-mind"), drinker, drinker, PopupType.Medium);
                }

                _audio.PlayPvs(vamp.DrinkSound, drinker);

                damage.DamageDict["Bloodloss"] = FixedPoint2.New(amount);
                _damage.TryChangeDamage(target, damage);

                var eui = new VampireRequestedEui(drinker, EntityManager, _actions, _vampireSystem, _prototypeManager);
                eui.GrantAbilities(drinker, vamp.SelectedSubgroup);

                // после того, как вампир выпивает кровь его глаза становятся красными
                TrySetEntityEyeColor(drinker, Color.Red);

                if (_mobState.IsAlive(target))
                    StartDrinking(drinker, target);

                return;
            }
        }

        // увеличиваем количество крови
        if (vamp != null)
        {
            vamp.BloodDamage = Math.Max(vamp.BloodDamage - amount, 0f);
            _vampireSystem.SetBloodCounterAlert(drinker, vamp);
            _vampireSystem.SetBloodAlert(drinker, vamp);
            vamp.TotalDrunk += amount;
            _audio.PlayPvs(vamp.DrinkSound, drinker);
        }
        else if (ghoul != null)
        {
            if (ghoul.BloodDamage - amount < 0)
            {
                _popup.PopupEntity(Loc.GetString("vampire-drinking-full-blood"), drinker, drinker, PopupType.Medium);
                return;
            }

            if (!HasComp<MindContainerComponent>(target) || !HasComp<ActorComponent>(target))
                ghoul.BloodDamage = Math.Max(ghoul.BloodDamage - amount / 2, 0);
            else
                ghoul.BloodDamage = Math.Max(ghoul.BloodDamage - amount, 0f);

            _vampireSystem.SetGhoulBloodAlert(drinker, ghoul);
            _audio.PlayPvs(ghoul.DrinkSound, drinker);
        }

        // после того, как вампир выпивает кровь его глаза становятся красными
        TrySetEntityEyeColor(drinker, Color.Red);

        // наносим жертве урон от кровопотери
        damage.DamageDict["Bloodloss"] = FixedPoint2.New(amount * 2);
        _damage.TryChangeDamage(target, damage);

        if (vamp != null)
        {
            var eui = new VampireRequestedEui(drinker, EntityManager, _actions, _vampireSystem, _prototypeManager);
            eui.GrantAbilities(drinker, vamp.SelectedSubgroup);
        }

        if (_mobState.IsAlive(target))
            StartDrinking(drinker, target);
    }

    // если вампир сдох, то освобождаем упырей от проклятия
    private void OnDead(Entity<VampireComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        foreach (var ghoul in ent.Comp.Ghouls)
        {
            if (!TryComp<ActorComponent>(ghoul, out var actor) || !TryComp<GhoulComponent>(ghoul, out var ghoulComponent))
                continue;

            RemoveMindFromGhoul(ghoul);
            _vampireSystem.SetGhoulBloodAlert(ghoul, ghoulComponent);
            var comp = EnsureComp<AbilityComponent>(ghoul);

            var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ghoul);
            transmitter.Channels.Remove(new ProtoId<RadioChannelPrototype>(comp.VampireRadioID));

            var activeRadio = EnsureComp<ActiveRadioComponent>(ghoul);
            activeRadio.Channels.Remove(new ProtoId<RadioChannelPrototype>(comp.VampireRadioID));

            // обновляем данные у вампира
            if (ent.Comp.Ghouls.Remove(ghoul))
            {
                ent.Comp.GhoulQuantity = Math.Max(0, ent.Comp.GhoulQuantity - 1);
                Dirty(ent, ent.Comp);
            }

            var eui = new VampireDeadEui();
            _eui.OpenEui(eui, actor.PlayerSession);
            RemComp<AbilityComponent>(ghoul);
        }
    }

    // выдаем вампиру cooldown на обращение
    public void AppealGhoulsCooldown(EntityUid uid)
    {
        if (!TryComp<VampireComponent>(uid, out var vampireComponent))
            return;
        var comp = EnsureComp<AbilityComponent>(uid);

        _statusEffects.TryAddStatusEffectDuration(uid,
            vampireComponent.CooldownStatusEffectAppealGhouls,
            out _,
            comp.CooldownTimeAppealGhouls,
            null);
    }

    private void OnSelectingSubgroup(VampireSelectingSubgroupEvent args)
    {
        if (!TryComp<ActorComponent>(args.Performer, out var actor))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        var eui = new VampireRequestedEui(args.Performer, EntityManager, _actions, _vampireSystem, _prototypeManager);
        _eui.OpenEui(eui, actor.PlayerSession);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        BaseUpdate(frameTime);
        AbilitiesUpdate();
    }

    public void BaseUpdate(float frameTime)
    {
        var querySearch = EntityQueryEnumerator<AbilityComponent>();
        while (querySearch.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<VampireComponent>(uid, out var vamp) && !TryComp<GhoulComponent>(uid, out var ghoul))
                continue;

            comp.UpdateDelay += frameTime;
            var priests = _lookup.GetEntitiesInRange<BibleUserComponent>(Transform(uid).Coordinates, 5).FirstOrNull();

            if (priests == null || _mobState.IsDead(priests.Value))
            {
                _alert.ClearAlert(uid, comp.AdjacentChaplainAlert);
                comp.UpdateDelay = 0f;
                continue;
            }

            if (comp.UpdateDelay < 1)
                continue;

            _damage.TryChangeDamage(uid, comp.DivineDamage);
            _audio.PlayPvs(comp.DivineDamageSound, uid);
            _popup.PopupEntity(Loc.GetString("vampire-popup-chaplain-closely"), uid, uid, PopupType.Medium);
            _alert.ShowAlert(uid, comp.AdjacentChaplainAlert);

            comp.UpdateDelay = 0;
        }
    }

    private void OnMindRemoved(VampireMindRemovedEvent ev)
    {
        if (_entityManager.GetEntity(ev.Uid) != null) VampireMindRemoved((EntityUid)_entityManager.GetEntity(ev.Uid)!);
    }

    public void VampireMindRemoved(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        _roleSystem.MindRemoveRole<VampireRoleComponent>((mindId, mind));

        var comp = EnsureComp<AbilityComponent>(uid);
        if (!TryComp<VampireComponent>(uid, out var vampireComponent)) return;

        foreach (var ghoul in vampireComponent.Ghouls)
        {
            if (!TryComp<GhoulComponent>(ghoul, out var ghoulComponent)) continue;

            RemoveMindFromGhoul(ghoul);

            // обновляем данные у вампира
            if (vampireComponent.Ghouls.Remove(ghoul))
            {
                vampireComponent.GhoulQuantity = Math.Max(0, vampireComponent.GhoulQuantity - 1);
                Dirty(uid, vampireComponent);
            }

            if (TryComp<ActorComponent>(ghoul, out var actor))
            {
                var eui = new VampireDeadEui();
                _eui.OpenEui(eui, actor.PlayerSession);
            }
        }

        if (comp.HaloUid != null) QueueDel(comp.HaloUid);
        if (comp.InvisibleIsActive) _vampireSystem.VampireInvisible(uid);
        if (comp.ItemIssued) _vampireSystem.OnIssuingSword(uid);

        if (vampireComponent.GrantedActions.Count > 0)
        {
            foreach (var action in vampireComponent.GrantedActions)
            {
                _actions.RemoveAction(uid, action);
            }
        }

        RemComp<VampireComponent>(uid);

        _alert.ClearAlert(uid, comp.AdjacentChaplainAlert);
        _vampireSystem.SetBloodCounterAlert(uid, vampireComponent);
        _vampireSystem.SetBloodAlert(uid, vampireComponent);
        if (vampireComponent.SelectingSubgroupActionEntity != null) _actions.RemoveAction(uid, vampireComponent.SelectingSubgroupActionEntity);

        RemComp<AbilityComponent>(uid);
    }

    private bool TrySetEntityEyeColor(EntityUid uid, Color eyeColor)
    {
        if (!_visualBodySystem.TryGatherMarkingsData(uid, null, out var profiles, out _, out var markings)) return false;

        var coloredProfile = profiles.ToDictionary(pair => pair.Key, pair => pair.Value with { EyeColor = eyeColor });
        _visualBodySystem.ApplyProfiles(uid, coloredProfile);

        return true;
    }
}
