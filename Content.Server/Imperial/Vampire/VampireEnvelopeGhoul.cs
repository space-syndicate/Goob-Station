using Content.Shared.Verbs;
using Content.Shared.Imperial.Vampire;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Content.Server.Mind;
using Robust.Shared.Player;
using Content.Server.Chat.Managers;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Rounding;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Stunnable;

namespace Content.Server.Imperial.Vampire;

public sealed class VampireEnvelopeGhoul : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);
        SubscribeLocalEvent<VampireComponent, VampireEnvelopeDoAfterEvent>(OnEnvelopeComplete);
    }

    private void OnGetVerbs(EntityUid uid, VampireComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || HasComp<GhoulComponent>(args.Target))
        {
            return;
        }

        // создаем верб для превращения цели в упыря
        var verb = new InnateVerb
        {
            Act = () =>
            {
                StartConversion(args.User, args.Target);
            },
            Text = Loc.GetString("vampire-verb-envelope-ghoul-text"),
            Message = Loc.GetString("vampire-verb-envelope-ghoul-message"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    private void StartConversion(EntityUid vampire, EntityUid target)
    {
        if (!TryComp<VampireComponent>(vampire, out var vampComp))
            return;

        var isSelf = vampire == target;

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-vampire-transform",
            ("target", MetaData(target).EntityName)),
            vampire, vampire, PopupType.MediumCaution);

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-ghoul-transform"),
            target, target, PopupType.LargeCaution);

        var doAfterArgs = new DoAfterArgs(EntityManager, vampire, TimeSpan.FromSeconds(5f),
            new VampireEnvelopeDoAfterEvent(), vampire, target: isSelf ? null : target)
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
    private void OnEnvelopeComplete(EntityUid uid, VampireComponent comp, VampireEnvelopeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var target = args.Target ?? args.User;

        if (HasComp<GhoulComponent>(target))
            return;

        ConvertToGhoul(args.User, target);
        args.Handled = true;
    }

    private void ConvertToGhoul(EntityUid vampire, EntityUid target)
    {
        var ghoulComp = EnsureComp<GhoulComponent>(target);
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

        SetGhoulBloodAlert(target, ghoulComp);
    }

    private void SetGhoulBloodAlert(EntityUid uid, GhoulComponent component)
    {
        // вычисляем, какой должен быть спрайт в зависимости от количества крови у упыря
        var severity = ContentHelpers.RoundToLevels(
            MathF.Max(0f, component.CritThreshold - component.BloodDamage),
            component.CritThreshold,
            7);
        _alerts.ShowAlert(uid, component.BloodAlert, (short)severity);
    }

    public void DealGhoulBloodDamage(EntityUid uid, float damage, GhoulComponent component)
    {
        component.BloodDamage = MathF.Min(component.BloodDamage + damage, component.CritThreshold);
        Dirty(uid, component);
        SetGhoulBloodAlert(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var ghoulQuery = EntityQueryEnumerator<GhoulComponent>();
        while (ghoulQuery.MoveNext(out var ghoulUid, out var ghoulComp))
        {
            if (ghoulComp.NextBloodDecay == TimeSpan.Zero)
            {
                ghoulComp.NextBloodDecay = _gameTiming.CurTime + ghoulComp.BloodDecayInterval;
                Dirty(ghoulUid, ghoulComp);
            }

            if (_gameTiming.CurTime >= ghoulComp.NextBloodDecay)
            {
                // наносим урон каждые BloodDecayInterval секунд
                DealGhoulBloodDamage(ghoulUid, ghoulComp.BloodDecayAmount, ghoulComp);
                ghoulComp.NextBloodDecay = _gameTiming.CurTime + ghoulComp.BloodDecayInterval;
                Dirty(ghoulUid, ghoulComp);

                // если урон больше количества крови, то применяем дебафы
                if (ghoulComp.BloodDamage >= ghoulComp.CritThreshold)
                {
                    if (TryComp<StaminaComponent>(ghoulUid, out var stamina))
                    {
                        var dmg = new DamageSpecifier();
                        dmg.DamageDict["Bloodloss"] = FixedPoint2.New(30);

                        _damage.TryChangeDamage(ghoulUid, dmg);
                        SpawnBloodPuddle(ghoulUid);
                        _stamina.TakeStaminaDamage(ghoulUid, 70f, stamina);
                        _jitterSystem.DoJitter(ghoulUid, ghoulComp.ShakingTime, refresh: true, amplitude: 15f, frequency: 4f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// спавн лужи крови
    /// </summary>
    private void SpawnBloodPuddle(EntityUid uid)
    {
        var coords = Transform(uid).Coordinates;

        if (_net.IsServer)
        {
            var puddle = Spawn("Puddle", coords);

            if (_solutionSystem.TryGetSolution(puddle, "puddle", out var solution))
            {
                var bloodSolution = new Solution();
                bloodSolution.AddReagent("Blood", 50f);

                _solutionSystem.TryAddSolution(solution.Value, bloodSolution);
            }
        }
    }
}
