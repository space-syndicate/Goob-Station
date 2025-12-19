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
using Content.Shared.Roles.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Stunnable;
using Content.Shared.Body.Components;
using Content.Server.Administration;
using Content.Shared.Chat;
using System.Linq;
using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using System.Numerics;
using Content.Shared.Actions.Components;
using Content.Server.Cloning;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.NPC;
using Content.Server.NPC;

namespace Content.Server.Imperial.Vampire;

public sealed class VampireServerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly VampireSystem _vampireSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbsCombined);
        SubscribeLocalEvent<GhoulComponent, GetVerbsEvent<InnateVerb>>(OnGetDrinkingGhoul);
        SubscribeLocalEvent<VampireComponent, VampireEnvelopeDoAfterEvent>(OnEnvelopeCompleteVampire);
        SubscribeLocalEvent<GhoulComponent, VampireDrinkingDoAfterEvent>(OnDrinkingCompleteGhoul);
        SubscribeLocalEvent<VampireComponent, VampireDrinkingDoAfterEvent>(OnDrinkingCompleteVampire);

        SubscribeLocalEvent<VampireMessageForGhouls>(MessageForGhouls);
        SubscribeLocalEvent<VampireBatTransformEvent>(OnTransformToBat);
        SubscribeLocalEvent<VampireCloneEvent>(OnClone);
    }

    private void OnGetVerbsCombined(EntityUid uid, VampireComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || comp.InvisibleIsActive)
            return;

        // если у цели нет крови (например, стул), кнопки не добавляем
        if (!HasComp<BloodstreamComponent>(args.Target))
            return;

        // верб для превращения цели в упыря
        if (!HasComp<GhoulComponent>(args.Target))
        {
            var verb1 = new InnateVerb
            {
                Act = () => StartConversion(args.User, args.Target),
                Text = Loc.GetString("vampire-verb-envelope-ghoul-text"),
                Message = Loc.GetString("vampire-verb-envelope-ghoul-message"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
                Priority = 1
            };
            args.Verbs.Add(verb1);
        }

        // верб для питья крови
        var verb2 = new InnateVerb
        {
            Act = () => StartDrinking(args.User, args.Target),
            Text = Loc.GetString("vampire-drinking-envelope-text"),
            Message = Loc.GetString("vampire-drinking-envelope-message"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
            Priority = 0
        };
        args.Verbs.Add(verb2);
    }

    private void OnGetDrinkingGhoul(EntityUid uid, GhoulComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // если у цели нет крови (например, стул), кнопки не добавляем
        if (!HasComp<BloodstreamComponent>(args.Target))
            return;

        var verb = new InnateVerb
        {
            Act = () =>
            {
                StartDrinking(args.User, args.Target);
            },
            Text = Loc.GetString("vampire-drinking-envelope-text"),
            Message = Loc.GetString("vampire-drinking-envelope-message"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
            Priority = 0
        };

        args.Verbs.Add(verb);
    }

    private void StartDrinking(EntityUid vampire, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, vampire, TimeSpan.FromSeconds(3f),
        new VampireDrinkingDoAfterEvent(), vampire, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void StartConversion(EntityUid vampire, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-vampire-transform",
            ("target", MetaData(target).EntityName)),
            vampire, vampire, PopupType.MediumCaution);

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-ghoul-transform"),
            target, target, PopupType.LargeCaution);

        var doAfterArgs = new DoAfterArgs(EntityManager, vampire, TimeSpan.FromSeconds(5f),
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
    private void OnEnvelopeCompleteVampire(EntityUid uid, VampireComponent comp, VampireEnvelopeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target
        || TryComp<GhoulComponent>(target, out var ghoulComp))
            return;

        ConvertToGhoul(args.User, target);
        args.Handled = true;
    }

    /// <summary>
    /// обработчик DoAfter для упыря
    /// </summary>
    private void OnDrinkingCompleteGhoul(Entity<GhoulComponent> ent, ref VampireDrinkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        DrinkingComplete(args.User, target, 20);
        args.Handled = true;
    }

    /// <summary>
    /// обработчик DoAfter для вампира
    /// </summary>
    private void OnDrinkingCompleteVampire(Entity<VampireComponent> ent, ref VampireDrinkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        DrinkingComplete(args.User, target, 20);
        args.Handled = true;
    }

    private void DrinkingComplete(EntityUid drinker, EntityUid target, float amount)
    {
        // проверяем, есть ли кровь у цели
        if (!TryComp<BloodstreamComponent>(target, out var blood) || blood.BloodSolution == null ||
            !TryComp<SolutionComponent>(blood.BloodSolution.Value.Owner, out var solutionComp))
        {
            _popup.PopupEntity(Loc.GetString("vampire-drinking-no-blood"), drinker, drinker, PopupType.LargeCaution);
            return;
        }

        var solution = solutionComp.Solution;

        if (solution.Volume.Float() < 20)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drinking-no-blood"), drinker, drinker, PopupType.LargeCaution);
            return;
        }

        TryComp<VampireComponent>(drinker, out var vamp);
        TryComp<GhoulComponent>(drinker, out var ghoul);

        if (vamp == null && ghoul == null)
            return;

        // вычисляем текущее количество крови
        float currentBlood = vamp != null ? vamp.CritThreshold - vamp.BloodDamage : ghoul!.CritThreshold - ghoul.BloodDamage;

        if (currentBlood >= 100)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drinking-full-blood"), drinker, drinker, PopupType.Medium);
            return;
        }

        // увеличиваем количество крови
        if (vamp != null)
        {
            vamp.BloodDamage = Math.Max(vamp.BloodDamage - amount, 0f);
            _vampireSystem.SetBloodAlert(drinker, vamp);
        }
        else
        {
            ghoul!.BloodDamage = Math.Max(ghoul.BloodDamage - amount, 0f);
            _vampireSystem.SetGhoulBloodAlert(drinker, ghoul);
        }

        // забираем кровь у жертвы
        var dmg = new DamageSpecifier();
        dmg.DamageDict["Bloodloss"] = FixedPoint2.New(20);
        _damage.TryChangeDamage(target, dmg);

        StartDrinking(drinker, target);
    }

    private void MessageForGhouls(VampireMessageForGhouls args)
    {
        if (!TryComp<ActorComponent>(args.Performer, out var actor))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.Ghouls == null || !vamp.Ghouls.Any())
        {
            _popup.PopupEntity(Loc.GetString("У вас нет упырей"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("Сообщение упырям"), Loc.GetString("Приказ"), (string message) =>
        {
            foreach (var ghoulUid in vamp.Ghouls)
            {
                if (!TryComp<ActorComponent>(ghoulUid, out var ghoulActor))
                    continue;

                _chatMan.ChatMessageToOne(
                    ChatChannel.Server,
                    $"[bold]Сообщение от вампира:\n{message}[/bold]",
                    $"[bold]Сообщение от вампира:\n{message}[/bold]",
                    args.Performer,
                    false,
                    ghoulActor.PlayerSession.Channel,
                    Color.DarkRed);
            }
        });
        args.Handled = true;
    }

    private void OnTransformToBat(VampireBatTransformEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupEntity(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.BuffBlocked)
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете стать летучей мышью под действием бафф способностей"),
            args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        if (vamp.DisguiseIsActive)
        {
            _popup.PopupEntity(Loc.GetString("Вы уже используете способность-маскировку"),
            args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var playerCoords = Transform(args.Performer).Coordinates;
        for (int i = 0; i < 3; i++)
        {
            EntityManager.SpawnEntity("MobVampireBat", playerCoords.Offset(new Vector2(i, 0)));
        }

        var config = new PolymorphConfiguration()
        {
            Entity = "MobVampireBat",
            Duration = 10,
            TransferName = true,
            TransferHumanoidAppearance = false,
            TransferDamage = true,
            Inventory = PolymorphInventoryChange.Transfer,
        };

        _polymorph.PolymorphEntity(args.Performer, config);

        vamp.VampireIsBat = true;
        vamp.DisguiseIsActive = true;

        _vampireSystem.DealBloodDamage(args.Performer, args.CostBlood);

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnClone(VampireCloneEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (!_prototypeManager.TryIndex(args.Settings, out var settings))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupEntity(Loc.GetString("Вам не хватает крови!"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var mapCoords = Transform(args.Performer).MapPosition;

        if (_cloning.TryCloning(args.Performer, mapCoords, settings, out var cloneUid))
        {
            // добавляем ai клону
            var htn = EnsureComp<HTNComponent>(cloneUid.Value);
            htn.RootTask = new HTNCompoundTask() { Task = "IdleCompound" };
            htn.Blackboard.SetValue(NPCBlackboard.Owner, cloneUid);

            EnsureComp<ActiveNPCComponent>(cloneUid.Value);
        }

        _vampireSystem.VampireInvisible(args.Performer);
        vamp.BuffBlockedUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(5);

        _vampireSystem.DealBloodDamage(args.Performer, args.CostBlood);

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }


    private void ConvertToGhoul(EntityUid vampire, EntityUid target)
    {
        var ghoulComp = EnsureComp<GhoulComponent>(target);
        ghoulComp.Vampire = vampire;

        var masterThralls = EnsureComp<VampireComponent>(vampire);
        masterThralls.Ghouls.Add(target);
        masterThralls.GhoulQuantity++;

        Dirty(vampire, masterThralls);
        Dirty(target, ghoulComp);

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-vampire-complete",
            ("target", MetaData(target).EntityName)),
            vampire, vampire, PopupType.LargeCaution);

        if (_mind.TryGetMind(target, out var mindId, out var mind))
        {
            // добавление роли и базовых эффектов для игрока
            if (!_roleSystem.MindHasRole<GhoulRoleComponent>(mindId))
                _roleSystem.MindAddRole(mindId, "MindRoleGhoul", mind: mind);

            if (_player.TryGetSessionById(mind.UserId, out var session))
            {
                _chatMan.DispatchServerMessage(session, Loc.GetString("vampire-verb-envelope-ghoul-greeting"));

                _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(5));
                _stun.TryKnockdown(target, TimeSpan.FromSeconds(5), force: true);
                _jitterSystem.DoJitter(target, ghoulComp.ShakingTime, refresh: true, amplitude: 25f, frequency: 8f);
            }
        }

        _vampireSystem.SetGhoulBloodAlert(target, ghoulComp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var queryVamp = EntityQueryEnumerator<VampireComponent>();
        while (queryVamp.MoveNext(out var uid, out var vamp))
        {
            if (!vamp.VampireIsBat)
                continue;

            if (!HasComp<PolymorphedEntityComponent>(uid))
            {
                vamp.VampireIsBat = false;
                vamp.DisguiseIsActive = false;
                Dirty(uid, vamp);
            }
        }

        var queryClone = EntityQueryEnumerator<VampireComponent>();
        while (queryClone.MoveNext(out var uid, out var vamp))
        {
            if (_gameTiming.CurTime >= vamp.BuffBlockedUntil && vamp.InvisibleIsActive)
            {
                _vampireSystem.VampireInvisible(uid);
                Dirty(uid, vamp);
            }
        }
    }

}
