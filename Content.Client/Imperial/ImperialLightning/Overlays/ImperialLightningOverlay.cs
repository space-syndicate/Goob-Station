using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Imperial.ImperialLightning;


/// <summary>
/// </summary>
public sealed class ImperialLightningOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private SharedTransformSystem _transformSystem = default!;


    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public override bool RequestScreenTexture => true;
    private List<ShaderInstance> _shaders = new();


    public List<Lightning> Lightnings = new();


    public ImperialLightningOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = _entityManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null) return;
        if (_playerManager.LocalEntity == null) return;

        var worldHandle = args.WorldHandle;

        for (var i = 0; i < Lightnings.ToList().Count; i++)
        {
            var lightning = Lightnings[i];

            if (CheckTargetAndStartPoints(lightning)) continue;
            if (!_shaders.TryGetValue(i, out var shader)) continue;

            if (lightning.DespawnTime <= _timing.CurTime)
                Lightnings.Remove(lightning);

            var startCoords = GetCoords(lightning.StartPoint);
            var targetCoords = GetCoords(lightning.TargetPoint);

            var height = Vector2.Distance(startCoords, targetCoords) / 2;
            var rotation = (targetCoords - startCoords).ToAngle();

            var lightningBounds = new Box2(
                -new Vector2(height),
                new Vector2(height)
            );

            var rotationMatrix = Matrix3Helpers.CreateRotation(rotation + Angle.FromDegrees(90));
            var lightningMatrix = Matrix3Helpers.CreateTranslation((startCoords + targetCoords) / 2);

            ApplyShaderParams(lightning, ref shader);

            worldHandle.UseShader(shader);
            worldHandle.SetTransform(
                Matrix3x2.Multiply(rotationMatrix, lightningMatrix)
            );

            worldHandle.DrawRect(lightningBounds, Color.White);

            worldHandle.UseShader(null);
            worldHandle.SetTransform(Matrix3x2.Identity);
        }
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        Lightnings.Clear();
    }

    #region Public API

    public void AddLightning(Lightning lightning)
    {
        Lightnings.Add(lightning);

        if (_shaders.TryGetValue(Lightnings.Count, out var _)) return;

        _shaders.Add(_prototypeManager.Index<ShaderPrototype>("Lightning").InstanceUnique());
    }

    public void AddLightning(
        (Vector2 StartCoords, EntityUid? StartEntityPoint) startPoint,
        (Vector2 TargetCoords, EntityUid? TargetEntityPoint) targetPoint,
        Vector3 lightningColor,
        Vector2 offset,
        float speed,
        float intensity,
        float seed,
        float amplitude,
        float frequency,
        TimeSpan despawnTime
    )
    {
        Lightnings.Add(new Lightning(startPoint, targetPoint, lightningColor, offset, speed, intensity, seed, amplitude, frequency, despawnTime));

        if (_shaders.TryGetValue(Lightnings.Count - 1, out var _)) return;

        _shaders.Add(_prototypeManager.Index<ShaderPrototype>("Lightning").InstanceUnique());
    }


    #endregion

    #region Helpers

    private Vector2 GetCoords((Vector2 Coords, EntityUid? CoordsEntity) coords)
    {
        return coords.CoordsEntity.HasValue
            ? _transformSystem.GetWorldPosition(coords.CoordsEntity.Value)
            : coords.Coords;
    }

    private bool CheckTargetAndStartPoints(Lightning lightning)
    {
        var startPointEnt = lightning.StartPoint.StartEntityPoint;
        var targetPointEnt = lightning.TargetPoint.TargetEntityPoint;

        if (startPointEnt.HasValue && _entityManager.Deleted(startPointEnt.Value)) return true;
        if (targetPointEnt.HasValue && _entityManager.Deleted(targetPointEnt.Value)) return true;

        return false;
    }

    private void ApplyShaderParams(Lightning lightning, ref ShaderInstance shader)
    {
        shader.SetParameter("lightning_color", lightning.LightningColor);
        shader.SetParameter("offset", lightning.Offset);
        shader.SetParameter("seed", lightning.Seed);
        shader.SetParameter("speed", lightning.Speed);
        shader.SetParameter("intensity", lightning.Intensity);
        shader.SetParameter("amplitude", lightning.Amplitude);
        shader.SetParameter("frequency", lightning.Frequency);
    }

    #endregion
}
