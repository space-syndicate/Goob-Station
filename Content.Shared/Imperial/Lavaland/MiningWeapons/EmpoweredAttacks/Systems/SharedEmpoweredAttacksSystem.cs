using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Events;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;
using Content.Shared.Coordinates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Systems;

public abstract class SharedEmpoweredAttacksSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    private string _earthshakerRiftSpawnPrototype = "EffectEarthshakerRiftSpawn";
    public override void Initialize()
    {
        base.Initialize();

        // Earthshaker
        SubscribeLocalEvent<UserEarthshakerStrikeComponent, EarthshakerStrikeEvent>(OnEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, GotEquippedHandEvent>(OnEquippedEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, GotUnequippedHandEvent>(OnUnequippedEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, ComponentShutdown>(OnEarthshakerShutdown);
        SubscribeLocalEvent<EarthshakerStrikeDoAfterEvent>(OnEarthshakerStrikeDoAfter);

        // Enhanced Bayonet
        SubscribeLocalEvent<UserEnhancedBayonetAttackComponent, EnhancedBayonetAttackEvent>(OnEnhancedBayonetAttack);
        SubscribeLocalEvent<EnhancedBayonetAttackComponent, GotEquippedHandEvent>(OnEquippedEnhancedBayonet);
        SubscribeLocalEvent<EnhancedBayonetAttackComponent, GotUnequippedHandEvent>(OnUnequippedEnhancedBayonet);
        SubscribeLocalEvent<EnhancedBayonetAttackComponent, ComponentShutdown>(OnEnhancedBayonetShutdown);

        // Enhanced Shot
        SubscribeLocalEvent<UserEnhancedShotComponent, EnhancedShotEvent>(OnEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, GotEquippedHandEvent>(OnEquippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, GotUnequippedHandEvent>(OnUnequippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, ComponentShutdown>(OnEnhancedShotShutdown);

        // Piercing Lunge
        SubscribeLocalEvent<UserPiercingLungeComponent, PiercingLungeEvent>(OnPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, GotEquippedHandEvent>(OnEquippedPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, GotUnequippedHandEvent>(OnUnequippedPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, ComponentShutdown>(OnPiercingLungeShutdown);
    }

    #region Earthshaker Strike

    private void OnEarthshakerStrike(EntityUid user, UserEarthshakerStrikeComponent comp, ref EarthshakerStrikeEvent args)
    {
        Log.Info($"EarthshakerStrike +");
        if (!comp.Item.HasValue)
            return;

        var time = 5.0f;

        if (!StartDoAfter(user, comp.Item.Value, time, new EarthshakerStrikeDoAfterEvent()))
            return;

        args.Handled = true;
    }

    private void OnEarthshakerStrikeDoAfter(EarthshakerStrikeDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            DoAfterCancelled(args.User);
            return;
        }

        if (args.Handled)
            return;

        Spawn(_earthshakerRiftSpawnPrototype, args.User.ToCoordinates());
        Log.Info("выполнено доафтер действие");

        args.Handled = true;
    }

    private void OnEquippedEarthshakerStrike(EntityUid uid, EarthshakerStrikeComponent comp, EquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionEarthshakerStrike);

        var userComp = EnsureComp<UserEarthshakerStrikeComponent>(args.User);
        userComp.Item = uid;

        comp.User = args.User;
    }

    private void OnUnequippedEarthshakerStrike(EntityUid uid, EarthshakerStrikeComponent comp, UnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserEarthshakerStrikeComponent>(args.User);
        }
    }

    private void OnEarthshakerShutdown(EntityUid uid, EarthshakerStrikeComponent comp, ComponentShutdown args)
    {
        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserEarthshakerStrikeComponent>(comp.User.Value);
        }
    }

    #endregion

    #region Enhanced Bayonet

    private void OnEnhancedBayonetAttack(EntityUid uid, UserEnhancedBayonetAttackComponent comp, ref EnhancedBayonetAttackEvent args)
    {
        Log.Info($"EnhancedBayonet +");
        args.Handled = true;
    }

    private void OnEquippedEnhancedBayonet(EntityUid uid, EnhancedBayonetAttackComponent comp, GotEquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionEnhancedBayonetAttack);
        AddComp<UserEnhancedBayonetAttackComponent>(args.User);

        comp.User = args.User;
    }

    private void OnUnequippedEnhancedBayonet(EntityUid uid, EnhancedBayonetAttackComponent comp, GotUnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserEnhancedBayonetAttackComponent>(args.User);
        }
    }

    private void OnEnhancedBayonetShutdown(EntityUid uid, EnhancedBayonetAttackComponent comp, ComponentShutdown args)
    {
        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserEnhancedBayonetAttackComponent>(comp.User.Value);
        }
    }

    #endregion

    #region Enhanced Shot

    private void OnEnhancedShot(EntityUid uid, UserEnhancedShotComponent comp, ref EnhancedShotEvent args)
    {
        Log.Info($"EnhancedShot +");
        args.Handled = true;
    }

    private void OnEquippedEnhancedShot(EntityUid uid, EnhancedShotComponent comp, GotEquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionEnhancedShot);
        AddComp<UserEnhancedShotComponent>(args.User);

        comp.User = args.User;
    }

    private void OnUnequippedEnhancedShot(EntityUid uid, EnhancedShotComponent comp, GotUnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserEnhancedShotComponent>(args.User);
        }
    }

    private void OnEnhancedShotShutdown(EntityUid uid, EnhancedShotComponent comp, ComponentShutdown args)
    {
        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserEnhancedShotComponent>(comp.User.Value);
        }
    }

    #endregion

    #region Piercing Lunge

    private void OnPiercingLunge(EntityUid uid, UserPiercingLungeComponent comp, ref PiercingLungeEvent args)
    {
        Log.Info($"PiercingLunge +");
        args.Handled = true;
    }

    private void OnEquippedPiercingLunge(EntityUid uid, PiercingLungeComponent comp, GotEquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionPiercingLunge);
        AddComp<UserPiercingLungeComponent>(args.User);

        comp.User = args.User;
    }

    private void OnUnequippedPiercingLunge(EntityUid uid, PiercingLungeComponent comp, GotUnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserPiercingLungeComponent>(args.User);
        }
    }

    private void OnPiercingLungeShutdown(EntityUid uid, PiercingLungeComponent comp, ComponentShutdown args)
    {
        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserPiercingLungeComponent>(comp.User.Value);
        }
    }

    #endregion

    #region Helpers

    private bool StartDoAfter(EntityUid user, EntityUid used, float time, DoAfterEvent doAfterEvent)
    {
        var args = new DoAfterArgs(EntityManager, user, time, doAfterEvent, used)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        };

        return _doAfter.TryStartDoAfter(args);
    }

    private void DoAfterCancelled(EntityUid user)
    {
        Log.Info("DoAfter сбит");

        _popup.PopupEntity(Loc.GetString("doafter-sbit"), user, user); //change
    }

    #endregion
}
