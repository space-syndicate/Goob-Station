using System.Numerics;
using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.ShockWave;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.Imperial.ShockWave;


public sealed class ShockWaveDistortionOverlay : Overlay, IEntityEventSubscriber
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private SharedTransformSystem? _xformSystem = null;

    private static string _shockWaveShaderProtoId = "ShockWave";


    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;


    private readonly ShaderInstance _shader;


    public const int MaxCount = 10;


    private readonly List<Vector2> _positions = new();
    private readonly List<float> _colors = new();
    private readonly List<float> _startTime = new();
    private readonly List<float> _speed = new();
    private readonly List<float> _distortion = new();
    private readonly List<float> _thikness = new();
    private readonly List<float> _maxDistance = new();
    private readonly List<float> _disappearDistancePercentage = new();

    public ShockWaveDistortionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index<ShaderPrototype>(_shockWaveShaderProtoId).Instance().Duplicate();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null) return false;
        if (_xformSystem is null && !_entityManager.TrySystem(out _xformSystem)) return false;

        _positions.Clear();
        _colors.Clear();
        _startTime.Clear();
        _speed.Clear();
        _distortion.Clear();
        _thikness.Clear();
        _maxDistance.Clear();
        _disappearDistancePercentage.Clear();

        var query = _entityManager.EntityQueryEnumerator<ShockWaveDistortionComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var component, out var xform))
        {
            if (xform.MapID != args.MapId) continue;

            var mapPos = _xformSystem.GetWorldPosition(uid);

            var tempCoords = args.Viewport.WorldToLocal(mapPos);
            tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;

            _colors.AddRange(ColorHelper.ToFloat3(component.Color));

            _positions.Add(tempCoords / args.Viewport.Size);
            _startTime.Add((float)(_timing.CurTime - component.SpawnTime).TotalMilliseconds);

            _speed.Add(component.Speed);
            _distortion.Add(component.Distortion);
            _thikness.Add(component.Thikness);
            _maxDistance.Add(component.MaxDistance);
            _disappearDistancePercentage.Add(component.DisappearDistancePercentage);

            if (_positions.Count == MaxCount) break;
        }

        return _positions.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader?.SetParameter("renderScale", args.Viewport.Eye.Scale);
        _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        _shader?.SetParameter("position", _positions.ToArray());
        _shader?.SetParameter("color", _colors.ToArray());
        _shader?.SetParameter("startTime", _startTime.ToArray());
        _shader?.SetParameter("speed", _speed.ToArray());
        _shader?.SetParameter("distortion", _distortion.ToArray());
        _shader?.SetParameter("thikness", _thikness.ToArray());
        _shader?.SetParameter("maxDistance", _maxDistance.ToArray());
        _shader?.SetParameter("disappearDistancePercentage", _disappearDistancePercentage.ToArray());

        _shader?.SetParameter("count", _positions.Count);

        var worldHandle = args.WorldHandle;

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldAABB, Color.White);

        worldHandle.UseShader(null);
    }
}
