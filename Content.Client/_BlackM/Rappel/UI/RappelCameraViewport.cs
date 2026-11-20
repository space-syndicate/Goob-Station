using System.Numerics;
using Content.Client.Viewport;
using Content.Shared.Mobs.Components;
using Content.Shared._BlackM.Rappel;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._BlackM.Rappel.UI
{
    public sealed class RappelCameraViewport : Control, IViewportControl
    {
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        private IClydeViewport? _viewport;
        private IEye? _eye;
        private Vector2i _viewportSize;
        private int _curRenderScale;
        private ScalingViewportStretchMode _stretchMode = ScalingViewportStretchMode.Bilinear;
        private ScalingViewportRenderScaleMode _renderScaleMode = ScalingViewportRenderScaleMode.Fixed;
        private ScalingViewportIgnoreDimension _ignoreDimension = ScalingViewportIgnoreDimension.None;
        private int _fixedRenderScale = 1;

        public ShaderInstance? PostProcessShader { get; set; }
        public RappelConsoleBoundUserInterfaceState? LandingPreview { get; set; }

        private readonly List<CopyPixelsDelegate<Rgba32>> _queuedScreenshots = new();

        public int CurrentRenderScale => _curRenderScale;

        public IEye? Eye
        {
            get => _eye;
            set
            {
                _eye = value;

                if (_viewport != null)
                    _viewport.Eye = value;
            }
        }

        public Vector2i ViewportSize
        {
            get => _viewportSize;
            set
            {
                if (_viewportSize == value)
                    return;
                _viewportSize = value;
                InvalidateViewport();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)] public Vector2i? FixedStretchSize { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public ScalingViewportStretchMode StretchMode
        {
            get => _stretchMode;
            set
            {
                _stretchMode = value;
                InvalidateViewport();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public ScalingViewportRenderScaleMode RenderScaleMode
        {
            get => _renderScaleMode;
            set
            {
                _renderScaleMode = value;
                InvalidateViewport();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public int FixedRenderScale
        {
            get => _fixedRenderScale;
            set
            {
                _fixedRenderScale = value;
                InvalidateViewport();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public ScalingViewportIgnoreDimension IgnoreDimension
        {
            get => _ignoreDimension;
            set
            {
                _ignoreDimension = value;
                InvalidateViewport();
            }
        }

        public RappelCameraViewport()
        {
            IoCManager.InjectDependencies(this);
            RectClipContent = true;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Handled)
                return;

            _inputManager.ViewportKeyEvent(this, args);
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Handled)
                return;

            _inputManager.ViewportKeyEvent(this, args);
        }

        protected override void Draw(IRenderHandle handle)
        {
            EnsureViewportCreated();

            DebugTools.AssertNotNull(_viewport);

            try
            {
                HideCharacters();
                _viewport!.Render();
            }
            finally
            {
                RestoreCharacters();
            }

            if (_queuedScreenshots.Count != 0)
            {
                var callbacks = _queuedScreenshots.ToArray();

                _viewport.RenderTarget.CopyPixelsToMemory<Rgba32>(image =>
                {
                    foreach (var callback in callbacks)
                    {
                        callback(image);
                    }
                });

                _queuedScreenshots.Clear();
            }

            var drawBox = GetDrawBox();

            if (PostProcessShader != null)
                handle.DrawingHandleScreen.UseShader(PostProcessShader);

            handle.DrawingHandleScreen.DrawTextureRect(_viewport.RenderTarget.Texture, drawBox);

            if (PostProcessShader != null)
                handle.DrawingHandleScreen.UseShader(null);

            if (LandingPreview is { } preview && preview.ZoneFound)
            {
                var screen = handle.DrawingHandleScreen;
                foreach (var point in preview.BlockedSlots)
                    DrawLandingMarker(screen, drawBox, point, Color.FromHex("#ed7777"), 0);
                foreach (var point in preview.ReservedSlots)
                    DrawLandingMarker(screen, drawBox, point, Color.FromHex("#efc16e"), 1);
                foreach (var point in preview.AvailableSlots)
                    DrawLandingMarker(screen, drawBox, point, Color.FromHex("#8bd9ac"), 2);
            }
        }

        private void DrawLandingMarker(DrawingHandleScreen screen, UIBox2i box, Vector2 world, Color color, int kind)
        {
            var local = _viewport!.WorldToLocal(world);
            var scale = box.Size / (Vector2) _viewport.Size;
            var point = (Vector2) box.TopLeft + local * scale;
            const float r = 5f;
            if (point.X < box.Left + r || point.X > box.Right - r ||
                point.Y < box.Top + r || point.Y > box.Bottom - r)
                return;
            if (kind == 0)
            {
                screen.DrawLine(point + new Vector2(-r, -r), point + new Vector2(r, r), color);
                screen.DrawLine(point + new Vector2(-r, r), point + new Vector2(r, -r), color);
            }
            else
            {
                screen.DrawCircle(point, r, color, false);
                if (kind == 2) screen.DrawCircle(point, 2f, color);
                else screen.DrawLine(point - new Vector2(r, 0f), point + new Vector2(r, 0f), color);
            }
        }

        private readonly List<(EntityUid Uid, SpriteComponent Sprite)> _hiddenCharacters = new();
        private readonly HashSet<EntityUid> _visited = new();
        private readonly Stack<EntityUid> _pending = new();

        private void HideCharacters()
        {
            var mobs = _entityManager.EntityQueryEnumerator<MobStateComponent, TransformComponent>();
            while (mobs.MoveNext(out var uid, out _, out var transform))
            {
                if (_eye == null || transform.MapID != _eye.Position.MapId)
                    continue;
                _pending.Push(uid);
            }

            var sprites = _entityManager.System<SpriteSystem>();
            while (_pending.TryPop(out var uid))
            {
                if (!_visited.Add(uid))
                    continue;
                if (_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite) && sprite.Visible)
                {
                    _hiddenCharacters.Add((uid, sprite));
                    sprites.SetVisible((uid, sprite), false);
                }
                if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var transform))
                    continue;
                var children = transform.ChildEnumerator;
                while (children.MoveNext(out var child))
                    _pending.Push(child);
            }
        }

        private void RestoreCharacters()
        {
            var sprites = _entityManager.System<SpriteSystem>();
            foreach (var (uid, sprite) in _hiddenCharacters)
            {
                if (!_entityManager.Deleted(uid))
                    sprites.SetVisible((uid, sprite), true);
            }
            _hiddenCharacters.Clear();
            _visited.Clear();
            _pending.Clear();
        }

        public void Screenshot(CopyPixelsDelegate<Rgba32> callback)
        {
            _queuedScreenshots.Add(callback);
        }

        private UIBox2i GetDrawBox()
        {
            DebugTools.AssertNotNull(_viewport);

            var vpSize = _viewport!.Size;
            var ourSize = (Vector2) PixelSize;

            if (FixedStretchSize == null)
            {
                var (ratioX, ratioY) = ourSize / vpSize;
                var ratio = 1f;
                switch (_ignoreDimension)
                {
                    case ScalingViewportIgnoreDimension.None:
                        ratio = Math.Min(ratioX, ratioY);
                        break;
                    case ScalingViewportIgnoreDimension.Vertical:
                        ratio = ratioX;
                        break;
                    case ScalingViewportIgnoreDimension.Horizontal:
                        ratio = ratioY;
                        break;
                }

                var size = vpSize * ratio;
                var pos = (ourSize - size) / 2;

                return (UIBox2i) UIBox2.FromDimensions(pos, size);
            }
            else
            {
                var pos = (ourSize - FixedStretchSize.Value) / 2;
                return (UIBox2i) UIBox2.FromDimensions(pos, FixedStretchSize.Value);
            }
        }

        private void RegenerateViewport()
        {
            DebugTools.AssertNull(_viewport);

            var vpSizeBase = ViewportSize;
            var ourSize = PixelSize;
            var (ratioX, ratioY) = ourSize / (Vector2) vpSizeBase;
            var ratio = Math.Min(ratioX, ratioY);
            var renderScale = 1;
            switch (_renderScaleMode)
            {
                case ScalingViewportRenderScaleMode.CeilInt:
                    renderScale = (int) Math.Ceiling(ratio);
                    break;
                case ScalingViewportRenderScaleMode.FloorInt:
                    renderScale = (int) Math.Floor(ratio);
                    break;
                case ScalingViewportRenderScaleMode.Fixed:
                    renderScale = _fixedRenderScale;
                    break;
            }

            renderScale = Math.Max(1, renderScale);

            _curRenderScale = renderScale;

            _viewport = _clyde.CreateViewport(
                ViewportSize * renderScale,
                new TextureSampleParameters
                {
                    Filter = StretchMode == ScalingViewportStretchMode.Bilinear,
                });

            _viewport.RenderScale = new Vector2(renderScale, renderScale);

            _viewport.Eye = _eye;
        }

        protected override void Resized()
        {
            base.Resized();

            InvalidateViewport();
        }

        private void InvalidateViewport()
        {
            _viewport?.Dispose();
            _viewport = null;
        }

        public MapCoordinates ScreenToMap(Vector2 coords)
        {
            if (_eye == null)
                return default;

            EnsureViewportCreated();

            Matrix3x2.Invert(GetLocalToScreenMatrix(), out var matrix);
            coords = Vector2.Transform(coords, matrix);

            return _viewport!.LocalToWorld(coords);
        }

        public MapCoordinates PixelToMap(Vector2 coords)
        {
            if (_eye == null)
                return default;

            EnsureViewportCreated();

            Matrix3x2.Invert(GetLocalToScreenMatrix(), out var matrix);
            coords = Vector2.Transform(coords, matrix);

            var ev = new PixelToMapEvent(coords, this, _viewport!);
            _entityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);

            return _viewport!.LocalToWorld(ev.VisiblePosition);
        }

        public Vector2 WorldToScreen(Vector2 map)
        {
            if (_eye == null)
                return default;

            EnsureViewportCreated();

            var vpLocal = _viewport!.WorldToLocal(map);

            var matrix = GetLocalToScreenMatrix();

            return Vector2.Transform(vpLocal, matrix);
        }

        public Matrix3x2 GetWorldToScreenMatrix()
        {
            EnsureViewportCreated();
            return _viewport!.GetWorldToLocalMatrix() * GetLocalToScreenMatrix();
        }

        public Matrix3x2 GetLocalToScreenMatrix()
        {
            EnsureViewportCreated();

            var drawBox = GetDrawBox();
            var scaleFactor = drawBox.Size / (Vector2) _viewport!.Size;

            if (scaleFactor.X == 0 || scaleFactor.Y == 0)
                return Matrix3x2.Identity;

            return Matrix3Helpers.CreateTransform(GlobalPixelPosition + drawBox.TopLeft, 0, scaleFactor);
        }

        private void EnsureViewportCreated()
        {
            if (_viewport == null)
            {
                RegenerateViewport();
            }

            DebugTools.AssertNotNull(_viewport);
        }
    }
}
