using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Cuffs.Components;
using System.Linq;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Stealth.Components;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Prototypes;


namespace Content.Shared.Imperial.Vampire;

public partial class SharedVampireSystem : EntitySystem
{
    public static readonly EntProtoId BlindingStatusEffect = "TemporaryBlindness";

    private void UmbraeInitialize()
    {
        SubscribeLocalEvent<VampireUnCuffEvent>(OnUnCuff);
        SubscribeLocalEvent<VampireInvisibleEvent>(OnInvisible);

        SubscribeLocalEvent<VampireBloodAnchorEvent>(OnBloodAnchorCreateStart);
        SubscribeLocalEvent<AbilityComponent, VampireAnchorCreateDoAfterEvent>(OnBloodAnchorCreate);

        SubscribeLocalEvent<AbilityComponent, VampireShadowTrapEvent>(StartOnShadowTrap);
        SubscribeLocalEvent<AbilityComponent, VampireShadowTrapDoAfterEvent>(OnShadowTrap);
        SubscribeLocalEvent<VampireTrapOnTriggerComponent, StartCollideEvent>(OnVampireTrap);
    }

    private void OnUnCuff(VampireUnCuffEvent args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var speed))
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        var comp = EnsureComp<AbilityComponent>(args.Performer);
        if (comp == null) return;

        if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
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
        DealAbilityBloodDamageShared(args.Performer, args.CostBlood);

        if (comp?.BuffBlocked ?? false)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-ability-buff"),
            args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        var boost = EnsureComp<VampireBuffComponent>(args.Performer);
        boost.BoostSpeed = args.UnCufBoostSpeed;
        Dirty(args.Performer, boost);

        _speedSystem.RefreshMovementSpeedModifiers(args.Performer);

        comp!.BuffBlocked = true;
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.UnCuffBuffTime;

        if (_net.IsServer)
            _jitterSystem.DoJitter(args.Performer, args.UnCuffBuffTime, refresh: false, amplitude: 2, frequency: 2);

        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void OnInvisible(VampireInvisibleEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        var comp = EnsureComp<AbilityComponent>(args.Performer);

        if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (comp.InvisibilityAbilityActive)
            comp.InvisibilityAbilityActive = false;
        else
            comp.InvisibilityAbilityActive = true;

        comp.BloodLossDisguiseIsActive = args.CostBlood;
        VampireInvisible(args.Performer);

        args.Handled = true;
    }

    private void OnBloodAnchorCreateStart(VampireBloodAnchorEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        var comp = EnsureComp<AbilityComponent>(args.Performer);

        if (!comp?.AnchorCreate ?? false)
        {
            if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
            {
                _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                    args.Performer, args.Performer, PopupType.Medium);
                return;
            }

            comp!.SpawnLocation = Transform(args.Performer).Coordinates;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, args.AnchorCreateTime,
                new VampireAnchorCreateDoAfterEvent { Duration = args.DurationExistenceAnchor, AnchorId = args.VampireAnchorId },
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
        if (comp!.VampireAnchorUid == EntityUid.Invalid || !Exists(comp.VampireAnchorUid))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-anchor-destroyed"),
                args.Performer, args.Performer, PopupType.LargeCaution);
            comp.AnchorCreate = false;
            return;
        }

        if (_net.IsServer)
        {
            _transform.SetCoordinates(args.Performer, Transform(comp.VampireAnchorUid).Coordinates);
            _audio.PlayPvs(comp.TeleportSound, args.Performer);

            QueueDel(comp.VampireAnchorUid);
        }

        comp.VampireAnchorUid = EntityUid.Invalid;
        comp.AnchorCreate = false;

        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void OnBloodAnchorCreate(Entity<AbilityComponent> ent, ref VampireAnchorCreateDoAfterEvent args)
    {
        if (!TryComp<VampireComponent>(ent, out var vamp) && !TryComp<GhoulComponent>(ent, out var ghoul)) return;
        if (args.Handled || args.Cancelled || !_net.IsServer) return;

        ent.Comp.VampireAnchorUid = Spawn(args.AnchorId, ent.Comp.SpawnLocation);
        ent.Comp.AnchorCreate = true;
        ent.Comp.AnchorDurationActive = _gameTiming.CurTime + args.Duration;

        Dirty(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void StartOnShadowTrap(Entity<AbilityComponent> ent, ref VampireShadowTrapEvent args)
    {
        if (!TryComp<VampireComponent>(ent, out var vamp) && !TryComp<GhoulComponent>(ent, out var ghoul)) return;
        if (args.Handled) return;

        var (uid, _) = ent;
        var user = args.Performer;

        if (!HasEnoughBloodShared(user, args.CostBlood))
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
        args.Handled = true;
    }

    private void OnShadowTrap(Entity<AbilityComponent> ent, ref VampireShadowTrapDoAfterEvent args)
    {
        if (!TryComp<VampireComponent>(ent, out var vamp) && !TryComp<GhoulComponent>(ent, out var ghoul)) return;
        if (args.Cancelled || args.Handled || !_net.IsServer) return;

        var targetPos = GetCoordinates(args.TargetCoords);
        ent.Comp.VampireUid = ent.Owner;

        Spawn(args.VampireTrapID, targetPos);

        args.Handled = true;
    }

    private void OnVampireTrap(Entity<VampireTrapOnTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<VampireComponent>(args.OtherEntity, out var vamp) && !TryComp<GhoulComponent>(args.OtherEntity, out var ghoul))
            return;

        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        _audio.PlayPredicted(ent.Comp.ShadowTrapSound, ent, ent);

        // ослепляем жертву
        if (_net.IsServer)
            _statusEffects.TryAddStatusEffectDuration(args.OtherEntity,
            BlindingStatusEffect, ent.Comp.BlindingTime);

        var dmg = new DamageSpecifier(ent.Comp.Damage);
        _damage.TryChangeDamage(args.OtherEntity, dmg);

        if (_net.IsServer)
            QueueDel(ent.Owner);
    }

    private void UmbraeUpdate()
    {
        var queryInvisible = EntityQueryEnumerator<AbilityComponent, StealthComponent>();
        while (queryInvisible.MoveNext(out var uid, out var comp, out var stealth))
        {
            if (!comp.InvisibilityAbilityActive)
                continue;

            float bloodDamage;
            if (TryComp<VampireComponent>(uid, out var vamp) && comp.VampireTurned) bloodDamage = 0f;
            else bloodDamage = comp.BloodLossDisguiseIsActive;

            if (!HasEnoughBloodShared(uid, bloodDamage))
            {
                VampireInvisible(uid);
                comp.InvisibilityAbilityActive = false;
                Dirty(uid, comp);
                continue;
            }

            if (comp.NextBloodDecayDisguise == TimeSpan.Zero)
            {
                comp.NextBloodDecayDisguise = _gameTiming.CurTime + comp.BloodDecayIntervalInvisible;
                Dirty(uid, comp);
            }

            if (_gameTiming.CurTime >= comp.NextBloodDecayDisguise)
            {
                DealAbilityBloodDamageShared(uid, comp.BloodLossDisguiseIsActive);
                comp.NextBloodDecayDisguise = _gameTiming.CurTime + comp.BloodDecayIntervalInvisible;
                Dirty(uid, comp);
            }
        }

        var vampAnchor = EntityQueryEnumerator<AbilityComponent>();
        while (vampAnchor.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime >= comp.AnchorDurationActive && comp.AnchorCreate)
            {
                if (_net.IsServer)
                    QueueDel(comp.VampireAnchorUid);

                // ссылаемся на VampireBloodAnchorAction. см VampireBaseAbilities, VampireUmbrae
                if (TryComp<VampireComponent>(uid, out var vampire)) _actions.SetCooldown(vampire.GrantedActions[7], comp.CooldownBloodAnchor);
                else if (TryComp<GhoulComponent>(uid, out var ghoul)) _actions.SetCooldown(_entityManager.GetEntity(ghoul.GhoulVampireBloodAnchorAction), comp.CooldownBloodAnchor);
                else return;
                _popup.PopupClient(Loc.GetString("vampire-popup-anchor-destroyed"),
                uid, uid, PopupType.LargeCaution);
                comp.AnchorCreate = false;

                Dirty(uid, comp);
            }
        }
    }
}
