using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Cuffs.Components;
using System.Linq;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Stealth.Components;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;


namespace Content.Shared.Imperial.Vampire;

public partial class SharedVampireSystem : EntitySystem
{
    private void UmbraeInitialize()
    {
        SubscribeLocalEvent<VampireUnCuffEvent>(OnUnCuff);
        SubscribeLocalEvent<VampireInvisibleEvent>(OnInvisible);

        SubscribeLocalEvent<VampireBloodAnchorEvent>(OnBloodAnchorCreateStart);
        SubscribeLocalEvent<VampireComponent, VampireAnchorCreateDoAfterEvent>(OnBloodAnchorCreate);

        SubscribeLocalEvent<VampireComponent, VampireShadowTrapEvent>(StartOnShadowTrap);
        SubscribeLocalEvent<VampireComponent, VampireShadowTrapDoAfterEvent>(OnShadowTrap);
        SubscribeLocalEvent<VampireTrapOnTriggerComponent, StartCollideEvent>(OnVampireTrap);
    }

    private void OnUnCuff(VampireUnCuffEvent args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (comp.BloodDamage + args.CostBlood >= comp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<CuffableComponent>(args.Performer, out var cuffComp) || cuffComp.CuffedHandCount <= 0)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-no-handcuffs"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        _cuff.Uncuff(args.Performer, args.Performer, cuffComp.Container.ContainedEntities.FirstOrDefault());

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        var boost = EnsureComp<VampireBuffComponent>(args.Performer);
        boost.BoostSpeed = args.UnCufBoostSpeed;
        Dirty(args.Performer, boost);

        _speedSystem.RefreshMovementSpeedModifiers(args.Performer);

        comp.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.UnCuffBuffTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.UnCuffBuffTime, refresh: false, amplitude: 2, frequency: 2);

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void OnInvisible(VampireInvisibleEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.InvisibilityAbilityActive)
            vamp.InvisibilityAbilityActive = false;
        else
            vamp.InvisibilityAbilityActive = true;

        vamp.BloodLossDisguiseIsActive = args.CostBlood;
        VampireInvisible(args.Performer);

        args.Handled = true;
    }

    private void OnBloodAnchorCreateStart(VampireBloodAnchorEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (!vamp.AnchorCreate)
        {
            if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                    args.Performer, args.Performer, PopupType.Medium);
                return;
            }

            vamp.SpawnLocation = Transform(args.Performer).Coordinates;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, args.AnchorCreateTime,
                new VampireAnchorCreateDoAfterEvent { Duration = args.DurationExistenceAnchor, AnchorId = args.VampireAnchorId},
                args.Performer)
            {
                BreakOnMove = false,
                BreakOnDamage = true,
                NeedHand = true,
                BlockDuplicate = true
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
            DealBloodDamage(args.Performer, args.CostBlood);
            return;
        }

        // проверка существования якоря
        if (vamp.VampireAnchorUid == EntityUid.Invalid || !Exists(vamp.VampireAnchorUid))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-anchor-destroyed"),
                args.Performer, args.Performer, PopupType.LargeCaution);
            vamp.AnchorCreate = false;
            return;
        }

        if (_net.IsServer)
        {
            _transform.SetCoordinates(args.Performer, Transform(vamp.VampireAnchorUid).Coordinates);
            _audio.PlayPvs(vamp.TeleportSound, args.Performer);

            QueueDel(vamp.VampireAnchorUid);
        }

        vamp.VampireAnchorUid = EntityUid.Invalid;
        vamp.AnchorCreate = false;

        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void OnBloodAnchorCreate(Entity<VampireComponent> ent, ref VampireAnchorCreateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_net.IsServer)
            return;

        ent.Comp.VampireAnchorUid = Spawn(args.AnchorId, ent.Comp.SpawnLocation);
        ent.Comp.AnchorCreate = true;
        ent.Comp.AnchorDurationActive = _gameTiming.CurTime + args.Duration;

        Dirty(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void StartOnShadowTrap(Entity<VampireComponent> ent, ref VampireShadowTrapEvent args)
    {
        if (args.Handled)
            return;

        var (uid, vamp) = ent;
        var user = args.Performer;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                user, user, PopupType.Medium);
            return;
        }

        // получаем координаты места по которому кликнул игрок
        var userPos = Transform(user).Coordinates;
        var targetPos = args.Target;

        if (!userPos.TryDistance(EntityManager, targetPos, out var distance) || distance > args.Radius)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-far-away"),
                user, user, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, args.DoAfterBeforeShadowTrap,
            new VampireShadowTrapDoAfterEvent { TargetCoords = GetNetCoordinates(targetPos), VampireTrapID = args.VampireTrapID },
            user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        DealBloodDamage(args.Performer, args.CostBlood);
    }

    private void OnShadowTrap(Entity<VampireComponent> ent, ref VampireShadowTrapDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !_net.IsServer)
            return;

        var targetPos = GetCoordinates(args.TargetCoords);
        ent.Comp.VampireUid = ent.Owner;

        Spawn(args.VampireTrapID, targetPos);

        args.Handled = true;
    }

    private void OnVampireTrap(Entity<VampireTrapOnTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (TryComp<VampireComponent>(args.OtherEntity, out var vamp))
            return;

        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        // ослепляем жертву
        _statusEffects.TryAddStatusEffect(args.OtherEntity, TemporaryBlindnessSystem.BlindingStatusEffect,
        ent.Comp.BlindingTime, false, TemporaryBlindnessSystem.BlindingStatusEffect);

        var dmg = new DamageSpecifier
        {
            DamageDict = { [ent.Comp.DamageType] = ent.Comp.Damage }
        };
        _damage.TryChangeDamage(args.OtherEntity, dmg);

        QueueDel(ent.Owner);
    }

    private void UmbraeUpdate()
    {
        var queryInvisible = EntityQueryEnumerator<VampireComponent, StealthComponent>();
        while (queryInvisible.MoveNext(out var uid, out var vamp, out var stealth))
        {
            if (!vamp.InvisibilityAbilityActive)
                continue;

            if (vamp.BloodDamage >= vamp.CritThreshold)
            {
                VampireInvisible(uid);
                vamp.InvisibilityAbilityActive = false;
                Dirty(uid, vamp);
                continue;
            }

            if (vamp.NextBloodDecayDisguise == TimeSpan.Zero)
            {
                vamp.NextBloodDecayDisguise = _gameTiming.CurTime + vamp.BloodDecayIntervalInvisible;
                Dirty(uid, vamp);
            }

            if (_gameTiming.CurTime >= vamp.NextBloodDecayDisguise)
            {
                DealBloodDamage(uid, vamp.BloodLossDisguiseIsActive);
                vamp.NextBloodDecayDisguise = _gameTiming.CurTime + vamp.BloodDecayIntervalInvisible;
                Dirty(uid, vamp);
            }
        }

        var vampAnchor = EntityQueryEnumerator<VampireComponent>();
        while (vampAnchor.MoveNext(out var uid, out var vamp))
        {
            if (_gameTiming.CurTime >= vamp.AnchorDurationActive && vamp.AnchorCreate)
            {
                if (_net.IsServer)
                    QueueDel(vamp.VampireAnchorUid);

                // ссылаемся на VampireJerkAction. см BaseAbilities, VampireAbilityLists.Umbrae
                _actions.SetCooldown(vamp.GrantedActions[6], vamp.CooldownBloodAnchor);
                _popup.PopupClient(Loc.GetString("vampire-popup-anchor-destroyed"),
                uid, uid, PopupType.LargeCaution);
                vamp.AnchorCreate = false;

                Dirty(uid, vamp);
            }
        }
    }
}
