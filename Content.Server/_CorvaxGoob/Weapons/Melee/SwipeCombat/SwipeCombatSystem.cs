using System.Linq;
using System.Numerics;
using Content.Server.Weapons.Melee;
using Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// Серверная логика свайп-атак: подбор целей, урон, притягивание/отталкивание и стаки.
/// </summary>
public sealed class SwipeCombatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MeleeSoundSystem _meleeSound = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SwipeAttackRequestEvent>(OnSwipeAttack);
    }

    private void OnSwipeAttack(SwipeAttackRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        // Атакуем только тем, что реально в руке, и только если это свайп-оружие.
        if (!_melee.TryGetWeapon(user, out var weaponUid, out var melee)
            || weaponUid != GetEntity(msg.Weapon)
            || !TryComp<SwipeCombatComponent>(weaponUid, out var swipe))
            return;

        if (!_combat.IsInCombatMode(user) || !_blocker.CanAttack(user, weapon: (weaponUid, melee)))
            return;

        var curTime = _timing.CurTime;
        if (melee.NextAttack > curTime)
            return;

        melee.NextAttack = curTime + TimeSpan.FromSeconds(1f / _melee.GetAttackRate(weaponUid, user, melee));
        DirtyField(weaponUid, melee, nameof(MeleeWeaponComponent.NextAttack));

        var userXform = Transform(user);
        var aimMap = _transform.ToMapCoordinates(GetCoordinates(msg.Coordinates));
        if (aimMap.MapId != userXform.MapID)
            return;

        var userPos = _transform.GetWorldPosition(userXform);
        var aimDir = aimMap.Position - userPos;
        if (aimDir.LengthSquared() < 0.0001f)
            aimDir = _transform.GetWorldRotation(userXform).ToWorldVec();
        aimDir = aimDir.Normalized();

        var isSlash = msg.Direction is SwipeAttackDirection.SlashLeft or SwipeAttackDirection.SlashRight;
        var (range, arc) = msg.Direction switch
        {
            SwipeAttackDirection.Overhead => (melee.Range * swipe.OverheadRangeMultiplier, swipe.OverheadAngle),
            SwipeAttackDirection.Thrust => (melee.Range * swipe.ThrustRangeMultiplier, swipe.ThrustAngle),
            _ => (melee.Range * swipe.SlashRangeMultiplier, swipe.SlashAngle),
        };

        var targets = FindTargets(user, weaponUid, melee, userPos, aimDir, arc, range, userXform.MapID);
        if (targets.Count == 0)
        {
            PlayLunge(user, weaponUid, userXform, userPos + aimDir, range, arc, msg.Direction, special: false);
            _meleeSound.PlaySwingSound(user, weaponUid, melee);
            return;
        }

        var anySpecial = false;

        if (!isSlash)
            targets.RemoveRange(1, targets.Count - 1);
        else if (targets.Count > swipe.MaxSlashTargets)
            targets.RemoveRange(swipe.MaxSlashTargets, targets.Count - swipe.MaxSlashTargets);

        _interaction.DoContactInteraction(user, weaponUid);

        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var special = RegisterHit(target, user, msg.Direction, swipe, curTime);
            anySpecial |= special;
            _interaction.DoContactInteraction(user, target);

            switch (msg.Direction)
            {
                case SwipeAttackDirection.Overhead:
                    DoOverhead(user, weaponUid, melee, swipe, target, userPos, special);
                    break;

                case SwipeAttackDirection.Thrust:
                    ApplyDamage(user, weaponUid, melee, target, swipe.ThrustDamageMultiplier, ignoreArmor: special, aimDir);
                    break;

                default:
                    var mult = i == 0 ? swipe.SlashFirstDamageMultiplier : swipe.SlashFollowupDamageMultiplier;
                    ApplyDamage(user, weaponUid, melee, target, mult, ignoreArmor: false, aimDir);
                    Knockback(user, target, userPos, special ? swipe.ComboSlashKnockback : swipe.SlashKnockback, swipe.KnockbackSpeed);
                    if (special)
                        _stamina.TakeStaminaDamage(target, swipe.ComboStaminaDamage, source: user, with: weaponUid, visual: true);
                    break;
            }

            if (special)
                _popup.PopupEntity(Loc.GetString($"swipe-combat-special-{msg.Direction.ToString().ToLowerInvariant()}"), target, PopupType.MediumCaution);
        }

        PlayLunge(user, weaponUid, userXform, userPos + aimDir, range, arc, msg.Direction, anySpecial);

        var attackEv = new MeleeAttackEvent(weaponUid);
        RaiseLocalEvent(user, ref attackEv);
    }

    /// <summary>
    /// Учитывает удар в стаках цели. Возвращает true, если этот удар — особый.
    /// </summary>
    private bool RegisterHit(EntityUid target, EntityUid user, SwipeAttackDirection direction, SwipeCombatComponent swipe, TimeSpan curTime)
    {
        var combo = EnsureComp<SwipeComboComponent>(target);

        if (combo.Attacker != user || curTime > combo.ExpiresAt || combo.LastDirection == null || combo.LastDirection == direction)
            combo.Stacks = 1;
        else
            combo.Stacks++;

        combo.Attacker = user;
        combo.LastDirection = direction;
        combo.ExpiresAt = curTime + swipe.ComboWindow;

        if (combo.Stacks < 2)
            return false;

        combo.Stacks = 0;
        return true;
    }

    private List<EntityUid> FindTargets(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent melee,
        Vector2 userPos, Vector2 aimDir, Angle arc, float range, MapId mapId)
    {
        var damageQuery = GetEntityQuery<DamageableComponent>();

        return _melee.ArcRayCast(userPos, aimDir.ToWorldAngle(), arc, range, mapId, user)
            .Where(ent => ent != user && ent != weaponUid && damageQuery.HasComponent(ent))
            .Where(ent => _blocker.CanAttack(user, ent, (weaponUid, melee)))
            .Where(ent =>
            {
                var before = new BeforeHarmfulActionEvent(user, HarmfulActionType.Harm);
                RaiseLocalEvent(ent, before);
                return !before.Cancelled;
            })
            .OrderBy(ent => (_transform.GetWorldPosition(ent) - userPos).LengthSquared())
            .ToList();
    }

    private void DoOverhead(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent melee, SwipeCombatComponent swipe,
        EntityUid target, Vector2 userPos, bool special)
    {
        var toUser = userPos - _transform.GetWorldPosition(target);
        var pullDistance = toUser.Length() - swipe.PullStopDistance;

        if (special)
            _stun.TryKnockdown(target, swipe.ComboKnockdownTime, refresh: true);

        if (pullDistance > 0.1f)
        {
            _throwing.TryThrow(target, toUser.Normalized() * pullDistance, swipe.PullSpeed, user,
                recoil: false, playSound: false, doSpin: false);
        }

        _meleeSound.PlayHitSound(target, user, null, null, melee);
        _adminLog.Add(LogType.MeleeHit, LogImpact.Low,
            $"{ToPrettyString(user):actor} pulled {ToPrettyString(target):subject} with swipe attack using {ToPrettyString(weaponUid):tool}{(special ? " (special: knockdown)" : "")}");
    }

    private void Knockback(EntityUid user, EntityUid target, Vector2 userPos, float distance, float speed)
    {
        var away = _transform.GetWorldPosition(target) - userPos;
        if (distance <= 0f || away.LengthSquared() < 0.0001f)
            return;

        _throwing.TryThrow(target, away.Normalized() * distance, speed, user,
            recoil: false, playSound: false, doSpin: false);
    }

    private void ApplyDamage(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent melee, EntityUid target,
        float multiplier, bool ignoreArmor, Vector2 aimDir)
    {
        var damage = _melee.GetDamage(weaponUid, user, melee) * multiplier;
        var targetCoords = Transform(target).Coordinates;

        var hitEvent = new MeleeHitEvent(new List<EntityUid> { target }, user, weaponUid, damage, aimDir, targetCoords);
        RaiseLocalEvent(weaponUid, hitEvent, true);
        if (hitEvent.Handled)
            return;

        var attacked = new AttackedEvent(weaponUid, user, targetCoords);
        RaiseLocalEvent(target, attacked);

        var modified = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attacked.BonusDamage, hitEvent.ModifiersList);
        modified = DamageSpecifier.ApplyModifierSets(modified, attacked.ModifiersList);

        if (melee.BypassFlatReductions || ignoreArmor)
            modified.BypassFlatReductions = true;

        var ignoreResistances = ignoreArmor || _melee.GetResistanceBypass(weaponUid, user, melee);

        if (_damageable.TryChangeDamage(target, modified, out var result, ignoreResistances: ignoreResistances, origin: user))
        {
            // Как у обычного ближнего боя: тупой урон бьёт и по стамине.
            if (result.DamageDict.TryGetValue("Blunt", out var blunt))
            {
                var ignoreResist = !TryComp<StaminaResistanceComponent>(target, out var staminaResist) || !staminaResist.MeleeResistance;
                _stamina.TakeStaminaDamage(target, (blunt * melee.BluntStaminaDamageFactor).Float(),
                    visual: false, source: user, with: weaponUid, ignoreResist: ignoreResist);
            }

            _adminLog.Add(LogType.MeleeHit, LogImpact.Medium,
                $"{ToPrettyString(user):actor} swipe attacked {ToPrettyString(target):subject} using {ToPrettyString(weaponUid):tool} and dealt {result.GetTotal():damage} damage{(ignoreArmor ? " (armor ignored)" : "")}");
        }

        _meleeSound.PlayHitSound(target, user, SharedMeleeWeaponSystem.GetHighestDamageSound(modified, _proto), hitEvent.HitSoundOverride, melee);

        if (result.GetTotal() > FixedPoint2.Zero)
            _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
    }

    /// <param name="worldPoint">Точка на 1 тайл в сторону удара.</param>
    private void PlayLunge(EntityUid user, EntityUid weaponUid, TransformComponent userXform,
        Vector2 worldPoint, float range, Angle arc, SwipeAttackDirection direction, bool special)
    {
        // Направление в системе координат родителя атакующего — так же, как считает штатный lunge.
        var localDir = Vector2.Transform(worldPoint, _transform.GetInvWorldMatrix(userXform));
        localDir = userXform.LocalRotation.RotateVec(localDir);

        // Только «рывок» персонажа: анимацию оружия (animation: null) рисует SwipeCombatVisualsSystem.
        // predicted: false — видит и сам атакующий.
        _melee.DoLunge(user, weaponUid, arc, localDir, null, Angle.Zero, false, predicted: false);

        RaiseNetworkEvent(
            new SwipeAttackVisualEvent(GetNetEntity(user), GetNetEntity(weaponUid), direction, localDir, range, arc, special),
            Filter.Pvs(user, entityManager: EntityManager));
    }
}
