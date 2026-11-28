using Content.Shared._CorvaxGoob.Surrender;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CorvaxGoob.Hostage;

public sealed partial class SharedHostageSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    public const float MinDistance = 1.5f;
    public const int HostagingTime = 5;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HostageTakerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    public void TryTakeHostage(EntityUid taker, EntityUid victim, EntityUid gun)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!CanHostage(taker, victim, gun))
            return;

        _popup.PopupPredicted(Loc.GetString("hostage-popup-taking-external", ("attacker", Identity.Entity(taker, EntityManager)), ("victim", Identity.Entity(victim, EntityManager))), victim, taker, type: Shared.Popups.PopupType.LargeCaution);

        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager, taker, HasComp<SurrenderComponent>(victim) ? 0 : SharedHostageSystem.HostagingTime, new StartTakeHostageDoAfterEvent(gun), victim, target: victim)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                MovementThreshold = 0.1f,
            });
    }

    private void OnRefreshMovementSpeed(Entity<HostageTakerComponent> entity, ref RefreshMovementSpeedModifiersEvent ev)
    {
        ev.ModifySpeed(entity.Comp.MovementSpeedMod);
    }

    public bool CanHostage(EntityUid taker, EntityUid victim, EntityUid gun)
    {
        if (taker == victim)
            return false;

        if (HasComp<ExecutionComponent>(gun) || HasComp<GunComponent>(gun))
        {
            if (!HasComp<DamageableComponent>(victim))
                return false;

            if (!TryComp<MobStateComponent>(victim, out var mobState))
                return false;

            if (HasComp<HostageComponent>(victim))
                return false;

            if (HasComp<HostageTakerComponent>(taker))
                return false;

            if (mobState.CurrentState != Mobs.MobState.Alive)
                return false;

            if (!_actionBlocker.CanAttack(taker, victim))
                return false;

            if (!_actionBlocker.CanInteract(taker, victim))
                return false;

            return true;
        }

        return false;
    }
}
