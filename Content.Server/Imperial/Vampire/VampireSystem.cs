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
using Content.Shared.Humanoid;
using Content.Server.EUI;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Robust.Server.Audio;

namespace Content.Server.Imperial.Vampire;

public partial class VampireSystem : EntitySystem
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
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    private void VampireInitialize()
    {
        SubscribeLocalEvent<VampireComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbsCombined);
        SubscribeLocalEvent<GhoulComponent, GetVerbsEvent<InnateVerb>>(OnGetDrinkingGhoul);
        SubscribeLocalEvent<GhoulComponent, InteractUsingEvent>(OnCureGhoulStart);
        SubscribeLocalEvent<GhoulComponent, VampireCureGhoulDoAfterEvent>(OnCureGhoul);
        SubscribeLocalEvent<VampireComponent, VampireEnvelopeDoAfterEvent>(OnEnvelopeCompleteVampire);
        SubscribeLocalEvent<GhoulComponent, VampireDrinkingDoAfterEvent>(OnDrinkingCompleteGhoul);
        SubscribeLocalEvent<VampireComponent, VampireDrinkingDoAfterEvent>(OnDrinkingCompleteVampire);
    }

    public override void Initialize()
    {
        base.Initialize();

        VampireInitialize();
        VampireAbilitiesInitialize();
    }

    private void OnGetVerbsCombined(EntityUid uid, VampireComponent vamp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || vamp.InvisibleIsActive || uid == args.Target || !_mobState.IsAlive(args.Target))
            return;

        // если у цели нет крови/разума, кнопки не добавляем
        if (!HasComp<BloodstreamComponent>(args.Target) || HasComp<MindContainerComponent>(args.Target)
            || HasComp<ActorComponent>(args.Target))
            return;

        // верб для превращения цели в упыря
        if (!HasComp<GhoulComponent>(args.Target))
        {
            var verbConvert = new InnateVerb
            {
                Act = () => StartConversion(args.User, args.Target),
                Text = Loc.GetString("vampire-verb-envelope-ghoul-text"),
                Message = Loc.GetString("vampire-verb-envelope-ghoul-message"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Mobs/Species/Human/organs.rsi/brain.png")),
                Priority = 0
            };
            args.Verbs.Add(verbConvert);
        }

        // верб для питья крови
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

        if (HasComp<VampireComponent>(drinker) && currentBlood >= 100 && vamp != null)
        {
            // мы просто засчитываем эту кровь в TotalDrunk, но BloodDamage не понижаем
            vamp.TotalDrunk += amount;
            _audio.PlayPvs(vamp.DrinkSound, drinker);

            damage.DamageDict["Bloodloss"] = FixedPoint2.New(amount * 2);
            _damage.TryChangeDamage(target, damage);

            var eui = new VampireRequestedEui(drinker, EntityManager, _actions, _vampireSystem, _prototypeManager);
            eui.GrantAbilities(drinker, vamp.SelectedSubgroup);

            // после того, как вампир выпивает кровь его глаза становятся красными
            if (TryComp<HumanoidAppearanceComponent>(drinker, out var humanoidAppearance))
            {
                humanoidAppearance.EyeColor = Color.Red;
                Dirty(drinker, humanoidAppearance);
            }

            if (_mobState.IsAlive(target))
                StartDrinking(drinker, target);

            return;
        }

        // увеличиваем количество крови
        if (vamp != null)
        {
            vamp.BloodDamage = Math.Max(vamp.BloodDamage - amount, 0f);
            _vampireSystem.SetBloodAlert(drinker, vamp);
            vamp.TotalDrunk += amount;
            _audio.PlayPvs(vamp.DrinkSound, drinker);
        }
        else if (ghoul != null)
        {
            ghoul.BloodDamage = Math.Max(ghoul.BloodDamage - amount, 0f);
            _vampireSystem.SetGhoulBloodAlert(drinker, ghoul);
            _audio.PlayPvs(ghoul.DrinkSound, drinker);
        }

        // после того, как вампир выпивает кровь его глаза становятся красными
        if (TryComp<HumanoidAppearanceComponent>(drinker, out var appear))
        {
            appear.EyeColor = Color.Red;
            Dirty(drinker, appear);
        }

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

    private void OnSelectingSubgroup(VampireSelectingSubgroupEvent args)
    {
        if (!TryComp<ActorComponent>(args.Performer, out var actor))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        var eui = new VampireRequestedEui(args.Performer, EntityManager, _actions, _vampireSystem, _prototypeManager);
        _eui.OpenEui(eui, actor.PlayerSession);
    }
}
