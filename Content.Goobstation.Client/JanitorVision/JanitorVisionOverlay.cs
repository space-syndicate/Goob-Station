// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Shared.JanitorVision;
using Content.Goobstation.Common.CCVar;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.JanitorVision;

/// <summary>Renders the actual cleaning targets with a thermal palette above lighting and FOV.</summary>
public sealed class JanitorVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eyes = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> ThermalShader = "JanitorVisionThermal";
    private static readonly ProtoId<ShaderPrototype> SceneShader = "JanitorVisionScene";

    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private readonly Dictionary<string, (Texture Texture, bool SnapCardinals)> _footprintTextures = new();
    private readonly SharedJanitorVisionSystem _vision;
    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprites;
    private readonly ShaderInstance _thermalShader;
    private readonly ShaderInstance _sceneShader;
    private readonly List<LayerState> _layerStates = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    public JanitorVisionComponent? Component;

    public float Intensity
    {
        get
        {
            var value = _configuration.GetCVar(GoobCVars.JanitorVisionIntensity);
            return float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : GoobCVars.JanitorVisionIntensity.DefaultValue;
        }
    }

    public JanitorVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _vision = _entities.System<SharedJanitorVisionSystem>();
        _transform = _entities.System<TransformSystem>();
        _sprites = _entities.System<SpriteSystem>();
        _thermalShader = _prototypes.Index(ThermalShader).InstanceUnique();
        _sceneShader = _prototypes.Index(SceneShader).InstanceUnique();
        ZIndex = 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) =>
        Component is { Enabled: true } && Intensity > 0f && args.Viewport.Eye == _eyes.CurrentEye;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Component is not { Enabled: true } comp || _player.LocalEntity is not { } observer
            || args.Viewport.Eye is not { } eye)
            return;

        // A remote camera must not project targets from the wearer's map onto another map.
        if (!_entities.TryGetComponent(observer, out TransformComponent? xform) || eye.Position.MapId != xform.MapID)
            return;

        var intensity = Intensity;
        if (intensity <= 0f)
            return;

        _thermalShader.SetParameter("strength", intensity);
        _sceneShader.SetParameter("strength", intensity);
        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        try
        {
            // Tint the existing view first so the thermal silhouettes retain their saturated colours.
            // This samples the already-rendered scene and does not reveal rooms outside normal FOV.
            if (ScreenTexture != null)
            {
                _sceneShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                handle.UseShader(_sceneShader);
                handle.DrawRect(args.WorldBounds, Color.White);
                handle.UseShader(null);
            }

            DrawFootprints(handle, observer, comp.Range, eye.Rotation);

            // Keep the same layering as the world: puddles below loose items.
            var puddles = _entities.EntityQueryEnumerator<PuddleComponent, SpriteComponent>();
            while (puddles.MoveNext(out var uid, out _, out var sprite))
                DrawTarget(handle, eye.Rotation, observer, uid, sprite, comp);

            var trash = _entities.EntityQueryEnumerator<TagComponent, SpriteComponent>();
            while (trash.MoveNext(out var uid, out _, out var sprite))
            {
                if (!_entities.HasComponent<PuddleComponent>(uid))
                    DrawTarget(handle, eye.Rotation, observer, uid, sprite, comp);
            }
        }
        finally
        {
            handle.UseShader(null);
            handle.SetTransform(Matrix3x2.Identity);
        }
    }

    private void DrawFootprints(DrawingHandleWorld handle, EntityUid observer, float range, Angle eyeRotation)
    {
        if (range <= 0f)
            return;

        var observerXform = _entities.GetComponent<TransformComponent>(observer);
        var observerPosition = _transform.GetWorldPosition(observerXform);
        var grids = _entities.EntityQueryEnumerator<DecalGridComponent, TransformComponent>();
        while (grids.MoveNext(out var grid, out var decals, out var xform))
        {
            if (xform.MapID != observerXform.MapID)
                continue;

            var localPosition = Vector2.Transform(observerPosition, _transform.GetInvWorldMatrix(xform));
            var extent = new Vector2(range + 1f);
            var bounds = new Box2(localPosition - extent, localPosition + extent);
            var chunks = new ChunkIndicesEnumerator(bounds, SharedDecalSystem.ChunkSize);
            handle.SetTransform(_transform.GetWorldMatrix(xform));
            handle.UseShader(_thermalShader);
            while (chunks.MoveNext(out var index))
            {
                if (!decals.ChunkCollection.ChunkCollection.TryGetValue(index.Value, out var chunk))
                    continue;

                foreach (var decal in chunk.Decals.Values)
                {
                    if (!_vision.TryGetFootprintTarget(observer, grid, decal, range, out _))
                        continue;

                    if (!_footprintTextures.TryGetValue(decal.Id, out var cache))
                    {
                        var prototype = _prototypes.Index<DecalPrototype>(decal.Id);
                        cache = (_sprites.Frame0(prototype.Sprite), prototype.SnapCardinals);
                        _footprintTextures[decal.Id] = cache;
                    }

                    var cardinal = cache.SnapCardinals
                        ? (eyeRotation + _transform.GetWorldRotation(xform)).GetCardinalDir().ToAngle()
                        : Angle.Zero;
                    handle.DrawTexture(cache.Texture, decal.Coordinates, decal.Angle - cardinal, Color.White);
                }
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTarget(DrawingHandleWorld handle, Angle eyeRotation, EntityUid observer, EntityUid target,
        SpriteComponent sprite, JanitorVisionComponent comp)
    {
        if (!sprite.Visible || !_vision.TryGetTarget(observer, target, comp.Range, out _, out var position))
            return;

        var originalColor = sprite.Color;
        _layerStates.Clear();
        try
        {
            var index = -1;
            foreach (var spriteLayer in sprite.AllLayers)
            {
                index++;
                if (spriteLayer is not SpriteComponent.Layer { Visible: true } layer)
                    continue;

                _layerStates.Add(new LayerState(index, layer.ShaderPrototype, layer.Shader, layer.Color));

                // Helper layers that feed stencil/displacement shaders are not part of the silhouette.
                if (layer.CopyToShaderParameters != null
                    || layer.ShaderPrototype?.Id is "DisplacedDraw" or "DisplacedStencilDraw")
                {
                    _sprites.LayerSetVisible(layer, false);
                    continue;
                }

                // Set every visible layer explicitly, including normally-unshaded puddle layers.
                sprite.LayerSetShader(index, _thermalShader, ThermalShader.Id);
                _sprites.LayerSetColor(layer, Color.White);
            }

            _sprites.SetColor((target, sprite), Color.White);
            _sprites.RenderSprite((target, sprite), handle, eyeRotation,
                _transform.GetWorldRotation(target), position);
        }
        finally
        {
            // The extra pass must never change the entity's normal appearance after taking the glasses off.
            _sprites.SetColor((target, sprite), originalColor);
            foreach (var state in _layerStates)
            {
                if (state.Prototype == SpriteSystem.UnshadedId)
                    sprite.LayerSetShader(state.Index, SpriteSystem.UnshadedId.Id);
                else
                    sprite.LayerSetShader(state.Index, state.Shader, state.Prototype?.Id);
                _sprites.LayerSetColor((target, sprite), state.Index, state.Color);
                _sprites.LayerSetVisible((target, sprite), state.Index, true);
            }
            handle.UseShader(null);
        }
    }

    private readonly record struct LayerState(
        int Index, ProtoId<ShaderPrototype>? Prototype, ShaderInstance? Shader, Color Color);
}
