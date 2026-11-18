using System.Numerics;
using Content.Shared._BlackM.Rappel;
using Content.Shared.Camera;
using Content.Client.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Random;
using Robust.Shared.Graphics;
using Robust.Shared.Map;

namespace Content.Client._BlackM.Rappel;

public sealed class RappelSystem : SharedRappelSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    private const float ShakeInterval = 0.035f;
    private const float SwingGravity = 42f;
    private const float SwingDamping = 0.12f;
    private const float SwingMaxAngle = 0.35f;
    private const float SwingStartKick = 0.5f;
    private const float SwingSlideKick = 0.35f;
    private const float SwingBrakeKick = 0.3f;
    private const float SwingLandBlend = 0.2f;
    private const float SwingStep = 1f / 120f;
    private const float SwingLeadTime = 1.2f;
    private const float SwingMaxCatchUp = 15f;
    public const float RopeTopExtra = 4f;
    private float _shakeAccumulator;

    private sealed class DescentVisual
    {
        public readonly RappellingComponent Rappel;
        public readonly SpriteComponent Source;
        public readonly bool Visible;
        public readonly EntityUid Proxy;
        public readonly SpriteComponent Sprite;
        public float Theta;
        public float AngularVelocity;
        public bool SwingStarted;
        public TimeSpan SimTime;
        public float Direction = 1f;
        public bool SlideKicked;
        public bool BrakeKicked;
        public float SwingAngle;
        public Vector2 Offset;
        public Vector2 Ground;
        public MapId Map = MapId.Nullspace;

        public DescentVisual(RappellingComponent rappel, SpriteComponent source,
            EntityUid proxy, SpriteComponent sprite)
        {
            Rappel = rappel;
            Source = source;
            Visible = source.Visible;
            Proxy = proxy;
            Sprite = sprite;
        }
    }

    private readonly Dictionary<EntityUid, DescentVisual> _visuals = new();
    private readonly List<EntityUid> _cleanup = new();

    public RappelConsoleBoundUserInterfaceState? ConsoleState;
    public EntityUid? ConsoleCamera;
    public IEye? ConsoleEye;
    public Vector2? ConsoleHover;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(Content.Client._BlackM.WalkBob.WalkBobSystem));
        SubscribeLocalEvent<RappellingComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        _overlay.AddOverlay(new RappelSelfOverlay());
        _overlay.AddOverlay(new RappelRopeOverlay());
        _overlay.AddOverlay(new RappelConsoleOverlay());
    }

    public override void Shutdown()
    {
        foreach (var uid in new List<EntityUid>(_visuals.Keys))
            RestoreVisual(uid);
        ClearConsole();
        _overlay.RemoveOverlay<RappelSelfOverlay>();
        _overlay.RemoveOverlay<RappelRopeOverlay>();
        _overlay.RemoveOverlay<RappelConsoleOverlay>();
        base.Shutdown();
    }

    private void OnGetEyeOffset(Entity<RappellingComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (IsAnimating(ent.Comp))
            args.Offset += GetDescentOffset(ent.Comp, Timing.CurTime) * 0.8f;
    }

    private bool IsAnimating(RappellingComponent rappel)
    {
        return rappel.LifeStage <= ComponentLifeStage.Running && !rappel.Preparing &&
            float.IsFinite(rappel.Duration) && rappel.Duration > 0f &&
            float.IsFinite(rappel.Height) &&
            GetProgress(rappel, Timing.CurTime) < 1f;
    }

    public void ClearConsole()
    {
        ConsoleState = null;
        ConsoleCamera = null;
        ConsoleEye = null;
        ConsoleHover = null;
    }

    public bool TryGetBody(EntityUid actor, out EntityUid proxy, out SpriteComponent sprite)
    {
        if (_visuals.TryGetValue(actor, out var visual) && visual.Visible &&
            !TerminatingOrDeleted(visual.Proxy))
        {
            proxy = visual.Proxy;
            sprite = visual.Sprite;
            return true;
        }
        proxy = default;
        sprite = default!;
        return false;
    }

    public bool TryGetSwing(EntityUid actor, out Vector2 offset)
    {
        if (_visuals.TryGetValue(actor, out var visual) && visual.SwingStarted)
        {
            offset = visual.Offset;
            return true;
        }
        offset = default;
        return false;
    }

    public bool TryGetRopeBody(Vector2 ropePosition, MapId map, out Vector2 harness, out float angle)
    {
        foreach (var visual in _visuals.Values)
        {
            if (visual.Map != map || Vector2.DistanceSquared(visual.Ground, ropePosition) > 0.25f)
                continue;
            harness = visual.Ground + visual.Offset + RappelVisualOffsets.HarnessOffset;
            angle = visual.SwingAngle;
            return true;
        }
        harness = default;
        angle = 0f;
        return false;
    }

    private float GetRopeLength(RappellingComponent rappel, TimeSpan time, out float drop)
    {
        var height = MathF.Max(0f, rappel.Height);
        drop = height * (1f - GetDescentTravel(GetProgress(rappel, time)));
        return MathF.Max(1f, height - drop + RopeTopExtra);
    }

    private void UpdateSwing(DescentVisual visual, RappellingComponent rappel, TimeSpan now)
    {
        var step = TimeSpan.FromSeconds(SwingStep);
        if (!visual.SwingStarted)
        {
            visual.SwingStarted = true;
            visual.SimTime = rappel.StartTime - TimeSpan.FromSeconds(SwingLeadTime);
            visual.Theta = 0f;
            visual.AngularVelocity = visual.Direction * SwingStartKick;
        }

        var catchUp = TimeSpan.FromSeconds(SwingMaxCatchUp);
        if (now - visual.SimTime > catchUp)
            visual.SimTime = now - catchUp;

        while (visual.SimTime + step <= now)
        {
            var t0 = visual.SimTime;
            var t1 = t0 + step;

            if (!visual.SlideKicked && t1 >= rappel.StartTime)
            {
                visual.SlideKicked = true;
                visual.AngularVelocity += visual.Direction * SwingSlideKick;
            }

            if (!visual.BrakeKicked && GetProgress(rappel, t1) >= 0.72f)
            {
                visual.BrakeKicked = true;
                var side = MathF.Abs(visual.AngularVelocity) > 0.01f
                    ? MathF.Sign(visual.AngularVelocity)
                    : MathF.Sign(visual.Direction);
                visual.AngularVelocity += side * SwingBrakeKick;
            }

            var l0 = GetRopeLength(rappel, t0, out _);
            var l1 = GetRopeLength(rappel, t1, out _);
            var length = (l0 + l1) * 0.5f;
            var lengthRate = (l1 - l0) / SwingStep;
            var accel = -SwingGravity / length * MathF.Sin(visual.Theta)
                - (2f * lengthRate / length + SwingDamping) * visual.AngularVelocity;
            visual.AngularVelocity += accel * SwingStep;
            visual.Theta += visual.AngularVelocity * SwingStep;
            if (MathF.Abs(visual.Theta) > SwingMaxAngle)
            {
                visual.Theta = MathF.CopySign(SwingMaxAngle, visual.Theta);
                visual.AngularVelocity = 0f;
            }
            visual.SimTime = t1;
        }

        var progress = GetProgress(rappel, now);
        var lengthNow = GetRopeLength(rappel, now, out var drop);
        var land = Math.Clamp((1f - progress) / SwingLandBlend, 0f, 1f);
        land = land * land * (3f - 2f * land);
        var angle = visual.Theta * land;
        visual.SwingAngle = angle;
        visual.Offset = new Vector2(lengthNow * MathF.Sin(angle), drop);
    }

    protected override void OnRappelShutdown(Entity<RappellingComponent> ent, ref ComponentShutdown args)
    {
        RestoreVisual(ent.Owner);
        base.OnRappelShutdown(ent, ref args);
        RefreshEye(ent.Owner);
    }

    private void RefreshEye(EntityUid uid)
    {
        if (!TerminatingOrDeleted(uid) && TryComp<EyeComponent>(uid, out var eye) &&
            eye.LifeStage <= ComponentLifeStage.Running)
            _contentEye.UpdateEyeOffset((uid, eye));
    }

    private void RestoreVisual(EntityUid uid)
    {
        if (!_visuals.Remove(uid, out var visual))
            return;
        visual.Rappel.OffsetSaved = false;
        if (!TerminatingOrDeleted(visual.Proxy))
        {
            _sprite.SetVisible((visual.Proxy, visual.Sprite), false);
            QueueDel(visual.Proxy);
        }
        if (!TerminatingOrDeleted(uid) && TryComp<SpriteComponent>(uid, out var sprite) &&
            ReferenceEquals(sprite, visual.Source) && sprite.LifeStage <= ComponentLifeStage.Running)
            _sprite.SetVisible((uid, sprite), visual.Visible);
        RefreshEye(uid);
    }

    private void UpdateCeilingBreaches(float frameTime)
    {
        _shakeAccumulator += frameTime;
        var shake = _shakeAccumulator >= ShakeInterval;
        if (shake)
            _shakeAccumulator = 0f;

        var now = Timing.CurTime;
        var local = _player.LocalEntity;
        var query = EntityQueryEnumerator<RappelCeilingBreachComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var breach, out var xform))
        {
            var elapsed = (float) (now - breach.StartTime).TotalSeconds;
            if (elapsed < 0f)
                continue;

            if (_lights.TryGetLight(uid, out var light))
            {
                var fade = Math.Clamp((breach.Duration - elapsed) / 2f, 0f, 1f);
                _lights.SetEnergy(uid, (1.2f + 4f * MathF.Exp(-elapsed * 3f)) * fade, light);
            }

            if (!shake || local is not { } player || elapsed > breach.ShakeDuration ||
                breach.ShakeDuration <= 0f || breach.ShakeRadius <= 0f)
                continue;

            var playerPos = _transform.GetMapCoordinates(player);
            var breachPos = _transform.GetMapCoordinates(uid, xform);
            if (playerPos.MapId != breachPos.MapId)
                continue;

            var distance = Vector2.Distance(playerPos.Position, breachPos.Position);
            if (distance > breach.ShakeRadius)
                continue;

            var falloff = 1f - distance / breach.ShakeRadius;
            var decay = 1f - elapsed / breach.ShakeDuration;
            var strength = breach.ShakeStrength * falloff * decay * decay * 0.25f;
            var angle = _random.NextFloat() * MathF.Tau;
            _recoil.KickCamera(player, new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * strength);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateCeilingBreaches(frameTime);
        _cleanup.Clear();
        foreach (var (uid, visual) in _visuals)
        {
            if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(visual.Proxy) ||
                !TryComp<RappellingComponent>(uid, out var rappel) ||
                !ReferenceEquals(rappel, visual.Rappel) || !IsAnimating(rappel) ||
                !TryComp<SpriteComponent>(uid, out var source) || !ReferenceEquals(source, visual.Source))
                _cleanup.Add(uid);
        }
        foreach (var uid in _cleanup)
            RestoreVisual(uid);

        var query = EntityQueryEnumerator<RappellingComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var rappel, out var source))
        {
            if (!IsAnimating(rappel))
            {
                rappel.OffsetSaved = false;
                RefreshEye(uid);
                continue;
            }
            if (!_visuals.TryGetValue(uid, out var visual))
            {
                var proxy = Spawn(null, Transform(uid).Coordinates);
                var proxySprite = EnsureComp<SpriteComponent>(proxy);
                _sprite.CopySprite((uid, source), (proxy, proxySprite));
                visual = new DescentVisual(rappel, source, proxy, proxySprite);
                visual.Direction = (MathF.Sin(rappel.SwaySeed) >= 0f ? 1f : -1f) *
                    (0.75f + 0.5f * MathF.Abs(MathF.Cos(rappel.SwaySeed)));
                _visuals.Add(uid, visual);
            }
            _sprite.SetVisible((uid, source), false);
            _sprite.SetVisible((visual.Proxy, visual.Sprite), visual.Visible && _player.LocalEntity != uid);
            var ground = _transform.GetMapCoordinates(uid);
            visual.Ground = ground.Position;
            visual.Map = ground.MapId;
            UpdateSwing(visual, rappel, Timing.CurTime);
            _transform.SetMapCoordinates(visual.Proxy,
                new MapCoordinates(ground.Position + visual.Offset, ground.MapId));
            _transform.SetWorldRotation(Transform(visual.Proxy), _transform.GetWorldRotation(uid));
            _sprite.SetRotation((visual.Proxy, visual.Sprite), source.Rotation);
            rappel.OriginalOffset = source.Offset;
            rappel.OffsetSaved = true;
            RefreshEye(uid);
        }
    }
}
