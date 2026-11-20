using System.Numerics;
using Content.Goobstation.Shared.Sprinting;
using Content.Shared._BlackM.WalkBob;
using Content.Shared._BlackM.Rappel;
using Content.Shared.Damage.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.WalkBob;

public sealed class WalkBobSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private const float KickCooldown = 0.05f;
    private const float FallbackSprintThreshold = 3.5f;
    private const float SpringStep = 1f / 120f;
    // Entities further than this from the camera are not animated (they are off-screen).
    private const float CullDistance = 24f;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<SprinterComponent> _sprinterQuery;
    private EntityQuery<StaminaComponent> _staminaQuery;
    private EntityQuery<MovementSpeedModifierComponent> _speedQuery;
    private EntityQuery<PullableComponent> _pullableQuery;
    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<RappellingComponent> _rappelQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _sprinterQuery = GetEntityQuery<SprinterComponent>();
        _staminaQuery = GetEntityQuery<StaminaComponent>();
        _speedQuery = GetEntityQuery<MovementSpeedModifierComponent>();
        _pullableQuery = GetEntityQuery<PullableComponent>();
        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _rappelQuery = GetEntityQuery<RappellingComponent>();

        SubscribeLocalEvent<WalkBobComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
        SubscribeNetworkEvent<MuzzleFlashEvent>(OnMuzzleFlash);
        SubscribeLocalEvent<PickupReachEvent>(OnPickupReach);
    }

    private void OnShutdown(EntityUid uid, WalkBobComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            Apply(uid, sprite, component, Vector2.One, Vector2.Zero, Angle.Zero);
    }

    private void OnPickupReach(PickupReachEvent ev)
    {
        EntityUid? best = null;
        var bestDist = 0.5f;

        var query = EntityQueryEnumerator<WalkBobComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != ev.MapId)
                continue;

            var dist = (_transform.GetWorldPosition(xform) - ev.To).Length();
            if (dist >= bestDist)
                continue;

            best = uid;
            bestDist = dist;
        }

        if (best is { } user)
            Reach(user, ev.To, ev.From);
    }

    public void Reach(EntityUid user, Vector2 userWorldPos, Vector2 targetWorldPos)
    {
        if (!TryComp<WalkBobComponent>(user, out var bob) || !CanAnimate(user))
            return;

        var dir = WorldToScreen(new MapCoordinates(userWorldPos, Transform(user).MapID), targetWorldPos - userWorldPos);
        bob.ReachDirection = dir.LengthSquared() > 0.0225f ? Vector2.Normalize(dir) : Vector2.Zero;
        bob.ReachTimeLeft = bob.ReachDuration;
    }

    public void Throw(EntityUid user, Vector2 worldDir)
    {
        if (!TryComp<WalkBobComponent>(user, out var bob) || !CanAnimate(user))
            return;

        var dir = WorldToScreen(_transform.GetMapCoordinates(user), worldDir);
        if (dir.LengthSquared() < 0.0001f)
            return;

        bob.ThrowDirection = Vector2.Normalize(dir);
        bob.ThrowTimeLeft = bob.ThrowDuration;
    }

    #region Recoil

    private void OnGunShot(EntityUid gunUid, GunComponent gun, ref GunShotEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var user = args.User;
        if (!HasComp<WalkBobComponent>(user))
            return;

        var dir = Vector2.Zero;
        if (gun.ShootCoordinates is { } coords && coords.IsValid(EntityManager))
            dir = _transform.ToMapCoordinates(coords).Position - _transform.GetMapCoordinates(user).Position;

        if (dir.LengthSquared() < 0.0001f)
            dir = FacingVector(user);

        Kick(user, Vector2.Normalize(dir), GunStrength(gun));
    }

    private void OnMuzzleFlash(MuzzleFlashEvent ev)
    {
        var gun = GetEntity(ev.Uid);
        if (!Exists(gun))
            return;

        var user = Transform(gun).ParentUid;
        if (!HasComp<WalkBobComponent>(user))
            return;

        var strength = TryComp<GunComponent>(gun, out var gunComp) ? GunStrength(gunComp) : 1f;
        Kick(user, FacingVector(user), strength);
    }

    private static float GunStrength(GunComponent gun)
    {
        var recoil = Math.Clamp(gun.CameraRecoilScalarModified, 0.5f, 1.5f);
        var rate = gun.FireRateModified > 0.01f ? gun.FireRateModified : gun.FireRate;
        var rateFactor = rate > 0.01f ? Math.Clamp(MathF.Sqrt(4f / rate), 0.6f, 1.2f) : 1f;
        return recoil * rateFactor;
    }

    private void Kick(EntityUid user, Vector2 worldDir, float strength)
    {
        if (!TryComp<WalkBobComponent>(user, out var bob) || !CanAnimate(user))
            return;

        if (_timing.CurTime < bob.NextKickTime)
            return;
        bob.NextKickTime = _timing.CurTime + TimeSpan.FromSeconds(KickCooldown);

        var dir = WorldToScreen(_transform.GetMapCoordinates(user), worldDir);
        if (dir.LengthSquared() < 0.0001f)
            return;
        dir = Vector2.Normalize(dir);

        var boost = MathF.Tau * bob.RecoilReturn * MathF.E;
        bob.RecoilVelocity -= dir * bob.RecoilKick * strength * boost;
        bob.RecoilRotationVelocity += dir.X * bob.RecoilTilt * strength * boost;
    }

    #endregion

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var dt = Math.Clamp(frameTime, 0.0001f, 0.1f);

        var eye = _eye.CurrentEye;
        var eyePos = eye.Position;
        var cull = CullDistance * MathF.Max(1f, MathF.Max(eye.Zoom.X, eye.Zoom.Y));
        var cullSq = cull * cull;

        var query = EntityQueryEnumerator<WalkBobComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var bob, out var sprite, out var xform))
        {
            if (xform.MapID != eyePos.MapId ||
                (_transform.GetWorldPosition(xform) - eyePos.Position).LengthSquared() > cullSq)
                continue;

            UpdateBob(uid, bob, sprite, xform, dt);
        }
    }

    private void UpdateBob(EntityUid uid, WalkBobComponent bob, SpriteComponent sprite, TransformComponent xform, float dt)
    {
        if (_rappelQuery.TryComp(uid, out var rappel) &&
            rappel.LifeStage <= ComponentLifeStage.Running)
        {
            Apply(uid, sprite, bob, Vector2.One, Vector2.Zero, Angle.Zero);
            return;
        }

        var alive = CanAnimate(uid);
        var k = 1f - MathF.Exp(-bob.Smoothing * dt);
        var mapCoords = _transform.GetMapCoordinates(uid, xform);

        var velocity = Vector2.Zero;
        var sprinting = false;
        if (bob.VisualSpeedOverride is { } over)
        {
            velocity = WorldToScreen(mapCoords, FacingVector(uid)) * over;
        }
        else if (alive && IsSelfMoving(uid) && _physicsQuery.TryComp(uid, out var physics))
        {
            var parentRot = xform.ParentUid.IsValid() ? _transform.GetWorldRotation(xform.ParentUid) : Angle.Zero;
            velocity = WorldToScreen(mapCoords, parentRot.RotateVec(physics.LinearVelocity));
            sprinting = _sprinterQuery.TryComp(uid, out var sprinter) && sprinter.IsSprinting;
        }

        var speed = velocity.Length();
        if (speed > bob.MaxAnimatedSpeed)
        {
            velocity = Vector2.Zero;
            speed = 0f;
        }

        var moving = alive && speed > bob.MinSpeedThreshold;
        var gait = moving ? GetGait(uid, bob, speed, sprinting) : 0f;

        bob.ActiveWeight = Lerp(bob.ActiveWeight, alive ? 1f : 0f, k);
        bob.MoveWeight = Lerp(bob.MoveWeight, moving ? 1f : 0f, k);
        bob.RunWeight = Lerp(bob.RunWeight, gait, k);

        if (moving)
            bob.Phase = (bob.Phase + speed * dt * bob.PhasePerMeter * Lerp(1f, bob.RunStrideMult, bob.RunWeight)) % MathF.Tau;
        else if (bob.MoveWeight < 0.02f)
            bob.Phase = 0f;

        var kick = 0f;
        if (moving != bob.WasMoving)
            kick = bob.SquashStartStop * (moving ? 1f : 0.5f + 0.5f * bob.RunWeight);
        bob.WasMoving = moving;

        // Soft start/stop settle.
        bob.SquashVelocity += kick * MathF.Tau * bob.SquashFrequency;
        Spring(ref bob.Squash, ref bob.SquashVelocity, 0f, bob.SquashFrequency, bob.SquashDamping, dt);

        // Smooth per-step wave, peak at footfall.
        var stepWave = 0.5f - 0.5f * MathF.Cos(2f * bob.Phase);
        var stepSquash = Lerp(bob.SquashWalk, bob.SquashRun, bob.RunWeight) * bob.MoveWeight * (stepWave - 0.3f);
        var totalSquash = Math.Clamp(bob.Squash + stepSquash, -bob.SquashMax * 0.5f, bob.SquashMax);

        if (gait > 0.5f)
            bob.Exertion = MathF.Min(1f, bob.Exertion + dt * (gait - 0.4f) / 0.6f / MathF.Max(bob.ExertionBuildTime, 0.01f));
        else
            bob.Exertion = MathF.Max(0f, bob.Exertion - dt / MathF.Max(bob.ExertionRecoverTime, 0.01f));

        var exertion = bob.Exertion;
        if (_staminaQuery.TryComp(uid, out var stamina) && stamina.CritThreshold > 0f)
            exertion = MathF.Max(exertion, Math.Clamp(stamina.StaminaDamage / stamina.CritThreshold * bob.StaminaExertion, 0f, 1f));

        if (!bob.BreathSeeded)
        {
            bob.BreathPhase = uid.Id * 0.7f % MathF.Tau;
            bob.BreathSeeded = true;
        }

        bob.BreathPhase = (bob.BreathPhase + Lerp(bob.BreathRate, bob.BreathRateExerted, exertion) * dt) % MathF.Tau;

        var omega = MathF.Tau * bob.RecoilReturn;
        CriticalSpring(ref bob.RecoilOffset.X, ref bob.RecoilVelocity.X, omega, dt);
        CriticalSpring(ref bob.RecoilOffset.Y, ref bob.RecoilVelocity.Y, omega, dt);
        CriticalSpring(ref bob.RecoilRotation, ref bob.RecoilRotationVelocity, omega, dt);

        if (bob.RecoilOffset.LengthSquared() > bob.RecoilMax * bob.RecoilMax)
            bob.RecoilOffset = Vector2.Normalize(bob.RecoilOffset) * bob.RecoilMax;

        var reach = 0f;
        if (bob.ReachTimeLeft > 0f)
        {
            bob.ReachTimeLeft = MathF.Max(0f, bob.ReachTimeLeft - dt);
            var u = 1f - bob.ReachTimeLeft / MathF.Max(bob.ReachDuration, 0.01f);
            reach = MathF.Sin(MathF.PI * MathF.Pow(u, 0.6f)) * bob.ActiveWeight;
        }

        var swing = 0f;
        if (bob.ThrowTimeLeft > 0f)
        {
            bob.ThrowTimeLeft = MathF.Max(0f, bob.ThrowTimeLeft - dt);
            var u = 1f - bob.ThrowTimeLeft / MathF.Max(bob.ThrowDuration, 0.01f);
            swing = MathF.Sin(MathF.PI * MathF.Pow(u, 0.35f)) * bob.ActiveWeight;
        }

        var w = bob.MoveWeight;
        var squash = totalSquash * bob.ActiveWeight;

        var bp = bob.BreathPhase;
        var breath = 0.5f - 0.5f * MathF.Cos(bp + 0.35f * MathF.Sin(bp));
        var breathAmp = Lerp(bob.BreathAmplitude, bob.BreathAmplitudeExerted, exertion)
                        * Lerp(1f, bob.BreathWhileMoving, w) * bob.ActiveWeight;

        var scaleY = (1f + breath * breathAmp) * (1f - squash) * (1f - bob.ReachDip * reach);
        var scaleX = (1f + breath * breathAmp * 0.25f) * (1f + squash * 0.6f);

        var rotDeg = bob.RecoilRotation
                     - bob.ReachDirection.X * bob.ReachAngle * reach
                     - bob.ThrowDirection.X * bob.ThrowAngle * swing;
        rotDeg = Math.Clamp(rotDeg, -bob.MaxRotation, bob.MaxRotation);

        var theta = rotDeg * MathF.PI / 180f;
        var foot = bob.FootOffset * scaleY;

        var offset = bob.RecoilOffset
                     + bob.ReachDirection * bob.ReachShift * reach
                     + bob.ThrowDirection * bob.ThrowShift * swing
                     + new Vector2(
                         -foot * MathF.Sin(theta),
                         -bob.FootOffset + foot * MathF.Cos(theta));

        Apply(uid, sprite, bob, new Vector2(scaleX, scaleY), offset, new Angle(theta));
    }

    // 0 walk, 0.6 run, 1 sprint
    private float GetGait(EntityUid uid, WalkBobComponent bob, float speed, bool sprinting)
    {
        if (sprinting)
            return 1f;

        var walk = 2.5f;
        var run = FallbackSprintThreshold + 1f;
        if (_speedQuery.TryComp(uid, out var mod))
        {
            walk = mod.CurrentWalkSpeed;
            run = mod.CurrentSprintSpeed;
        }

        if (bob.SprintSpeedThreshold is { } threshold)
            run = threshold;

        if (run <= walk * 1.05f)
            return Math.Clamp(speed / MathF.Max(run, 0.1f), 0f, 1f) * 0.6f;

        var gait = Math.Clamp((speed - walk) / (run - walk), 0f, 1f) * 0.6f;
        if (speed > run * 1.1f)
            gait = MathF.Max(gait, 0.6f + 0.4f * Math.Clamp((speed - run) / MathF.Max(run * 0.35f, 0.1f), 0f, 1f));

        return gait;
    }

    private bool IsSelfMoving(EntityUid uid)
    {
        if (_pullableQuery.TryComp(uid, out var pullable) && pullable.BeingPulled)
            return false;

        if (_buckleQuery.TryComp(uid, out var buckle) && buckle.Buckled)
            return false;

        return true;
    }

    private bool CanAnimate(EntityUid uid)
    {
        if (_mobStateQuery.TryComp(uid, out var mobState) && mobState.CurrentState != MobState.Alive)
            return false;

        return !_standing.IsDown(uid);
    }

    private void Apply(EntityUid uid, SpriteComponent sprite, WalkBobComponent bob,
        Vector2 scale, Vector2 offset, Angle rotation)
    {
        int? layer = null;
        if (bob.TargetLayerKey != null && sprite.LayerMapTryGet(bob.TargetLayerKey, out var index))
            layer = index;

        var onLayer = layer != null;
        if (onLayer != bob.AppliedOnLayer)
        {
            bob.AppliedScale = Vector2.One;
            bob.AppliedOffset = Vector2.Zero;
            bob.AppliedRotation = Angle.Zero;
            bob.WrittenScale = Vector2.One;
            bob.WrittenOffset = Vector2.Zero;
            bob.WrittenRotation = Angle.Zero;
            bob.AppliedOnLayer = onLayer;
        }

        if (ApproxEq(scale, bob.AppliedScale)
            && ApproxEq(offset, bob.AppliedOffset)
            && MathF.Abs((float) (rotation - bob.AppliedRotation).Theta) < 0.0001f)
        {
            return;
        }

        Vector2 curScale;
        Vector2 curOffset;
        Angle curRotation;

        if (layer is { } l)
        {
            var layerData = (SpriteComponent.Layer) sprite[l];
            curScale = layerData.Scale;
            curOffset = layerData.Offset;
            curRotation = layerData.Rotation;
        }
        else
        {
            curScale = sprite.Scale;
            curOffset = sprite.Offset;
            curRotation = sprite.Rotation;
        }

        var baseScale = ApproxEq(curScale, bob.WrittenScale)
            ? new Vector2(SafeDiv(curScale.X, bob.AppliedScale.X), SafeDiv(curScale.Y, bob.AppliedScale.Y))
            : curScale;
        var baseOffset = ApproxEq(curOffset, bob.WrittenOffset) ? curOffset - bob.AppliedOffset : curOffset;
        var baseRotation = MathF.Abs((float) (curRotation - bob.WrittenRotation).Theta) < 0.0001f
            ? curRotation - bob.AppliedRotation
            : curRotation;

        var newScale = baseScale * scale;
        var newOffset = baseOffset + offset;
        var newRotation = baseRotation + rotation;

        if (layer is { } li)
        {
            _sprite.LayerSetScale((uid, sprite), li, newScale);
            _sprite.LayerSetOffset((uid, sprite), li, newOffset);
            _sprite.LayerSetRotation((uid, sprite), li, newRotation);
        }
        else
        {
            _sprite.SetScale((uid, sprite), newScale);
            _sprite.SetOffset((uid, sprite), newOffset);
            _sprite.SetRotation((uid, sprite), newRotation);
        }

        bob.AppliedScale = scale;
        bob.AppliedOffset = offset;
        bob.AppliedRotation = rotation;
        bob.WrittenScale = newScale;
        bob.WrittenOffset = newOffset;
        bob.WrittenRotation = newRotation;
    }

    #region Helpers

    private static void Spring(ref float x, ref float v, float target, float frequency, float damping, float dt)
    {
        var w = MathF.Tau * frequency;
        var steps = Math.Max(1, (int) MathF.Ceiling(dt / SpringStep));
        var h = dt / steps;

        for (var i = 0; i < steps; i++)
        {
            var acc = -w * w * (x - target) - 2f * damping * w * v;
            v += acc * h;
            x += v * h;
        }
    }

    private static void CriticalSpring(ref float x, ref float v, float omega, float dt)
    {
        var steps = Math.Max(1, (int) MathF.Ceiling(dt / SpringStep));
        var h = dt / steps;

        for (var i = 0; i < steps; i++)
        {
            var acc = -omega * omega * x - 2f * omega * v;
            v += acc * h;
            x += v * h;
        }

        if (MathF.Abs(x) < 1e-5f && MathF.Abs(v) < 1e-4f)
        {
            x = 0f;
            v = 0f;
        }
    }

    private Vector2 WorldToScreen(MapCoordinates origin, Vector2 worldVec)
    {
        var length = worldVec.Length();
        if (length < 1e-5f)
            return Vector2.Zero;

        if (origin.MapId != _eye.CurrentEye.Position.MapId)
            return (-_eye.CurrentEye.Rotation).RotateVec(worldVec);

        var a = _eye.MapToScreen(origin).Position;
        var b = _eye.MapToScreen(new MapCoordinates(origin.Position + worldVec / length, origin.MapId)).Position;
        var screen = new Vector2(b.X - a.X, a.Y - b.Y);

        return screen.LengthSquared() < 1e-6f ? Vector2.Zero : Vector2.Normalize(screen) * length;
    }

    private Vector2 FacingVector(EntityUid uid)
    {
        return _transform.GetWorldRotation(uid).GetDir().ToVec();
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float SafeDiv(float value, float divisor)
        => MathF.Abs(divisor) > 1e-4f ? value / divisor : value;

    private static bool ApproxEq(Vector2 a, Vector2 b)
        => Vector2.DistanceSquared(a, b) < 1e-8f;

    #endregion
}
