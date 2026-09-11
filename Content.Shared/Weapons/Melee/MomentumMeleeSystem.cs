using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Handles <see cref="MomentumMeleeComponent"/>: a held item that damages mobs it is
/// carried into, scaled by the holder's movement speed (Minecraft-spear style).
/// </summary>
public sealed class MomentumMeleeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    // Reused per-tick scratch buffers to avoid allocations.
    private readonly HashSet<Entity<MobStateComponent>> _targets = new();
    private readonly List<EntityUid> _staleCooldowns = new();

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = AllEntityQuery<MomentumMeleeComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // The item must be held in an actual hand (not a pocket/bag, which also
            // parent to the same mob). A held item is parented to the holder.
            var holder = xform.ParentUid;

            if (!holder.IsValid() || !_hands.IsHolding(holder, uid))
            {
                if (comp.LastHit.Count > 0)
                    comp.LastHit.Clear();
                continue;
            }

            // Cheap velocity gate before doing any lookups.
            if (!_physicsQuery.TryGetComponent(holder, out var physics))
                continue;

            // Use map-frame velocity so it works while riding a vehicle / standing on a
            // moving grid: the holder is parented to the vehicle and its own LinearVelocity
            // is ~0, but the real speed is that of the parent chain.
            var velocity = _physics.GetMapLinearVelocity(holder, physics);
            var speed = velocity.Length();

            if (speed < comp.MinimumSpeed)
                continue;

            var moveDir = velocity / speed; // normalized
            var holderPos = _transform.GetWorldPosition(holder);
            var cosHalfArc = (float) Math.Cos(Angle.FromDegrees(comp.ArcDegrees).Theta / 2);

            _targets.Clear();
            _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(holder), comp.Range, _targets);

            foreach (var target in _targets)
            {
                if (target.Owner == holder || target.Owner == uid)
                    continue;

                if (!_mobState.IsAlive(target.Owner, target.Comp))
                    continue;

                // Forward-cone filter: target must lie in the direction of movement.
                var toTarget = _transform.GetWorldPosition(target.Owner) - holderPos;

                if (toTarget.LengthSquared() > 0f)
                {
                    if (Vector2.Dot(moveDir, Vector2.Normalize(toTarget)) < cosHalfArc)
                        continue;
                }

                // Per-target cooldown.
                if (comp.LastHit.TryGetValue(target.Owner, out var last)
                    && (now - last).TotalSeconds < comp.DamageCooldown)
                    continue;

                comp.LastHit[target.Owner] = now;

                var damageScale = comp.SpeedDamageFactor * speed / comp.MinimumSpeed;
                _damageable.TryChangeDamage(target.Owner, comp.Damage * damageScale, origin: holder);

                if (comp.SoundHit != null && _timing.IsFirstTimePredicted)
                    _audio.PlayPvs(comp.SoundHit, target.Owner, AudioParams.Default.WithVariation(0.125f));

                _color.RaiseEffect(Color.Red, new List<EntityUid> { target.Owner }, Filter.Pvs(target.Owner, entityManager: EntityManager));

                // Optionally embed the weapon into the target, dropping it from the hand.
                if (comp.EmbedOnHit
                    && TryComp<EmbeddableProjectileComponent>(uid, out var embeddable)
                    && embeddable.EmbeddedIntoUid == null)
                {
                    _hands.TryDrop(holder, uid, checkActionBlocker: false, doDropInteraction: false);
                    _projectile.EmbedAttach(uid, target.Owner, holder, embeddable);
                    break; // weapon left the hand; stop scanning further targets
                }
            }

            CleanupCooldowns(comp, now);
        }
    }

    /// <summary>
    /// Drops stale cooldown entries so the dictionary doesn't grow unbounded.
    /// </summary>
    private void CleanupCooldowns(MomentumMeleeComponent comp, TimeSpan now)
    {
        if (comp.LastHit.Count == 0)
            return;

        var cooldown = comp.DamageCooldown;
        _staleCooldowns.Clear();

        foreach (var (target, last) in comp.LastHit)
        {
            if ((now - last).TotalSeconds >= cooldown)
                _staleCooldowns.Add(target);
        }

        foreach (var target in _staleCooldowns)
        {
            comp.LastHit.Remove(target);
        }
    }
}
