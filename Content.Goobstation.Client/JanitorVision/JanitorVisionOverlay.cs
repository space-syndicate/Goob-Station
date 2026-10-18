// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Shared.JanitorVision;
using Content.Shared.Fluids.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.JanitorVision;

/// <summary>Renders the actual cleaning targets with a thermal palette above lighting and FOV.</summary>
public sealed class JanitorVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eyes = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> ThermalShader = "ThermalVision";
    private static readonly ProtoId<ShaderPrototype> SceneShader = "NightVision";

    private readonly SharedJanitorVisionSystem _vision;
    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprites;
    private readonly ShaderInstance _thermalShader;
    private readonly ShaderInstance _sceneShader;
    private readonly List<LayerState> _layerStates = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    public JanitorVisionComponent? Component;

    public JanitorVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _vision = _entities.System<SharedJanitorVisionSystem>();
        _transform = _entities.System<TransformSystem>();
        _sprites = _entities.System<SpriteSystem>();
        _thermalShader = _prototypes.Index(ThermalShader).Instance();
        _sceneShader = _prototypes.Index(SceneShader).InstanceUnique();
        _sceneShader.SetParameter("tint", new Vector3(0.12f, 0.38f, 0.12f));
        _sceneShader.SetParameter("luminance_threshold", 2f);
        _sceneShader.SetParameter("noise_amount", 0.15f);
        ZIndex = 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => args.Viewport.Eye == _eyes.CurrentEye;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Component is not { Enabled: true } comp || _player.LocalEntity is not { } observer
            || args.Viewport.Eye is not { } eye)
            return;

        // A remote camera must not project targets from the wearer's map onto another map.
        if (!_entities.TryGetComponent(observer, out TransformComponent? xform) || eye.Position.MapId != xform.MapID)
            return;

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
