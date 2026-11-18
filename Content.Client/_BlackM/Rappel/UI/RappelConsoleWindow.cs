using System.Numerics;
using Content.Client.Eye;
using Content.Client.Viewport;
using Content.Shared._BlackM.Rappel;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using CameraEye = Robust.Shared.Graphics.Eye;

namespace Content.Client._BlackM.Rappel.UI;

public sealed class RappelConsoleWindow : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public event Action<Vector2>? OnSelectPoint;
    public event Action? OnClearPoint;
    public event Action<Vector2>? OnMoveCamera;
    public event Action? OnRecenter;

    private static readonly TimeSpan CameraSendInterval = TimeSpan.FromMilliseconds(60);
    private const float MinZoom = 1f;
    private const float ZoomLimit = 1.5f;

    private readonly EyeLerpingSystem _lerp;
    private readonly RappelSystem _rappel;
    private readonly SharedTransformSystem _xform;

    private readonly RappelCameraViewport _viewport;
    private bool _signal;
    private bool _zoneFound;
    private Vector2 _zoneCenter;
    private float _zoneRadius;
    private readonly RappelViewportInput _input;
    private readonly Label _noSignal;
    private readonly Button _clearButton;
    private readonly FixedEye _defaultEye = new();
    private CameraEye? _localEye;

    private NetEntity? _cameraNet;
    private EntityUid? _camera;
    private float _zoom = 1f;
    private float _maxZoom = ZoomLimit;
    private bool _zoomInitialized;

    private Vector2? _pendingCamera;
    private Vector2? _lastSentCamera;
    private TimeSpan _nextCameraSend;

    public RappelConsoleWindow()
    {
        IoCManager.InjectDependencies(this);

        _lerp = _entMan.System<EyeLerpingSystem>();
        _rappel = _entMan.System<RappelSystem>();
        _xform = _entMan.System<SharedTransformSystem>();

        Title = Loc.GetString("rappel-console-window-title");
        MinSize = new Vector2(520, 420);
        SetSize = new Vector2(720, 560);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
            Margin = new Thickness(6),
        };

        var viewBox = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            ToolTip = Loc.GetString("rappel-console-controls") + "\n" + Loc.GetString("rappel-console-preview-legend"),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#141619"),
                BorderColor = Color.FromHex("#42464d"),
                BorderThickness = new Thickness(1),
            },
        };
        _viewport = new RappelCameraViewport
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MouseFilter = MouseFilterMode.Ignore,
            Eye = _defaultEye,
            StretchMode = ScalingViewportStretchMode.Bilinear,
            Visible = false,
        };
        _viewport.PostProcessShader = _prototypes.Index<ShaderPrototype>("Greyscale").InstanceUnique();
        _viewport.OnResized += ResizeViewport;
        ResizeViewport();
        _noSignal = new Label
        {
            Text = Loc.GetString("rappel-console-no-signal"),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontColorOverride = Color.FromHex("#c4c9cf"),
        };
        _input = new RappelViewportInput
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MouseFilter = MouseFilterMode.Stop,
        };
        viewBox.AddChild(_viewport);
        viewBox.AddChild(_noSignal);
        viewBox.AddChild(_input);

        var footer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        var recenter = MinimalButton(Loc.GetString("rappel-console-recenter"));
        recenter.OnPressed += _ =>
        {
            _pendingCamera = _rappel.ConsoleState?.Target ?? _zoneCenter;
            _lastSentCamera = null;
            UpdateLocalPosition();
            _zoom = 1f;
            ApplyZoom();
            OnRecenter?.Invoke();
        };
        _clearButton = MinimalButton(Loc.GetString("rappel-console-clear"));
        _clearButton.Disabled = true;
        _clearButton.OnPressed += _ => OnClearPoint?.Invoke();
        footer.AddChild(recenter);
        footer.AddChild(_clearButton);
        footer.AddChild(new Control { HorizontalExpand = true });
        root.AddChild(viewBox);
        root.AddChild(footer);
        Contents.AddChild(root);

        _input.LeftClicked += OnLeftClicked;
        _input.Dragged += OnDragged;
        _input.Hovered += OnHovered;
        _input.HoverLost += () => _rappel.ConsoleHover = null;
        _input.Wheel += OnWheel;

        OnClose += Cleanup;
    }

    public void UpdateState(RappelConsoleBoundUserInterfaceState state)
    {
        _rappel.ConsoleState = state;
        _viewport.LandingPreview = state;
        _maxZoom = float.IsFinite(state.MaxZoom) ? Math.Clamp(state.MaxZoom, MinZoom, ZoomLimit) : ZoomLimit;
        _zoneFound = state.ZoneFound;
        _zoneCenter = state.ZoneCenter;
        _zoneRadius = state.ZoneRadius;

        if (!_zoomInitialized)
        {
            _zoom = float.IsFinite(state.CameraZoom) ? Math.Clamp(state.CameraZoom, MinZoom, _maxZoom) : 1f;
            _zoomInitialized = true;
        }

        _zoom = Math.Clamp(_zoom, MinZoom, _maxZoom);

        if (_cameraNet != state.Camera)
        {
            DetachCamera();
            _cameraNet = state.Camera;
        }

        _clearButton.Disabled = !state.ZoneFound || state.Target == null;
        ApplyZoom();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_camera == null && _cameraNet != null && _entMan.TryGetEntity(_cameraNet, out var cam))
        {
            _camera = cam;
            _lerp.AddEye(cam.Value);
            _rappel.ConsoleCamera = cam;
            ApplyZoom();
        }

        if (_camera != null && _entMan.TryGetComponent<EyeComponent>(_camera, out var eye) && eye.Eye != null)
        {
            var remote = eye.Eye;
            _localEye ??= new CameraEye();
            _localEye.Position = remote.Position;
            _localEye.Rotation = remote.Rotation;
            _localEye.Offset = remote.Offset;
            _localEye.DrawFov = remote.DrawFov;
            _localEye.DrawLight = true;
            _viewport.Eye = _localEye;
            _rappel.ConsoleEye = _localEye;
            UpdateLocalPosition();
            ApplyZoom();
            _noSignal.Visible = !_zoneFound;
            _signal = _zoneFound;
            _viewport.Visible = _signal;
        }
        else
        {
            _viewport.Eye = _defaultEye;
            _rappel.ConsoleEye = null;
            _noSignal.Visible = true;
            _signal = false;
            _viewport.Visible = false;
        }

        if (_signal && _pendingCamera is { } pending && _timing.RealTime >= _nextCameraSend && pending != _lastSentCamera)
        {
            OnMoveCamera?.Invoke(pending);
            _lastSentCamera = pending;
            _nextCameraSend = _timing.RealTime + CameraSendInterval;
        }
    }

    private void OnLeftClicked(Vector2 screenPos)
    {
        if (!InsideFeed(screenPos))
            return;
        var map = _viewport.PixelToMap(screenPos);
        if (map.MapId == MapId.Nullspace || _camera == null ||
            Vector2.Distance(map.Position, _zoneCenter) > _zoneRadius)
            return;

        OnSelectPoint?.Invoke(map.Position);
    }

    private void OnDragged(Vector2 from, Vector2 to)
    {
        if (!_signal || _camera is not { } camera || !_entMan.TryGetComponent<TransformComponent>(camera, out var xform))
            return;

        var a = _viewport.PixelToMap(from);
        var b = _viewport.PixelToMap(to);
        if (a.MapId == MapId.Nullspace || b.MapId == MapId.Nullspace)
            return;

        var basePos = _pendingCamera ?? _xform.GetWorldPosition(xform);
        var desired = basePos + (a.Position - b.Position);
        var offset = desired - _zoneCenter;
        if (offset.Length() > _zoneRadius)
            desired = _zoneCenter + Vector2.Normalize(offset) * _zoneRadius;
        _pendingCamera = desired;
        UpdateLocalPosition();
    }

    private void UpdateLocalPosition()
    {
        if (_localEye == null || _pendingCamera is not { } position)
            return;

        var offset = position - _zoneCenter;
        if (offset.LengthSquared() > _zoneRadius * _zoneRadius)
            position = _zoneCenter + Vector2.Normalize(offset) * _zoneRadius;
        _pendingCamera = position;
        _localEye.Position = new MapCoordinates(position, _localEye.Position.MapId);
    }

    private void OnHovered(Vector2 screenPos)
    {
        if (!InsideFeed(screenPos))
        {
            _rappel.ConsoleHover = null;
            return;
        }
        var map = _viewport.PixelToMap(screenPos);
        _rappel.ConsoleHover = map.MapId == MapId.Nullspace ? null : map.Position;
    }

    private void OnWheel(float delta)
    {
        _zoom = Math.Clamp(_zoom - delta * 0.1f, MinZoom, _maxZoom);
        ApplyZoom();
    }

    private bool InsideFeed(Vector2 screenPos)
    {
        var local = screenPos - (Vector2) _viewport.GlobalPixelPosition;
        return _signal && local.X >= 0 && local.Y >= 0 &&
            local.X < _viewport.PixelWidth && local.Y < _viewport.PixelHeight;
    }

    private void ApplyZoom()
    {
        if (_viewport.Eye != null)
            _viewport.Eye.Zoom = new Vector2(_zoom, _zoom);
    }

    private void ResizeViewport()
    {
        var width = Math.Max(_viewport.PixelWidth, 32);
        var height = Math.Max(_viewport.PixelHeight, 32);
        var scale = 360f / Math.Min(width, height);
        _viewport.ViewportSize = new Vector2i(Math.Max(32, (int) (width * scale)), Math.Max(32, (int) (height * scale)));
    }

    private static Button MinimalButton(string text) => new()
    {
        Text = text,
        MinHeight = 30,
        StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#30343a"),
            BorderColor = Color.FromHex("#4a5058"),
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
            ContentMarginTopOverride = 4,
            ContentMarginBottomOverride = 4,
        },
    };

    private void DetachCamera()
    {
        if (_camera != null && !_entMan.Deleted(_camera))
            _lerp.RemoveEye(_camera.Value);

        _camera = null;
        _localEye = null;
        _rappel.ConsoleEye = null;
        _signal = false;
        _pendingCamera = null;
        _lastSentCamera = null;
        _rappel.ConsoleHover = null;
        _rappel.ConsoleCamera = null;
        _viewport.Eye = _defaultEye;
    }

    private void Cleanup()
    {
        DetachCamera();
        _rappel.ClearConsole();
    }
}

public sealed class RappelViewportInput : Control
{
    public event Action<Vector2>? LeftClicked;
    public event Action<Vector2, Vector2>? Dragged;
    public event Action<Vector2>? Hovered;
    public event Action? HoverLost;
    public event Action<float>? Wheel;

    private bool _dragging;
    private Vector2 _last;

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            LeftClicked?.Invoke(args.PointerLocation.Position);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _dragging = true;
            _last = args.PointerLocation.Position;
            args.Handle();
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIRightClick)
            return;

        _dragging = false;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        var pos = args.GlobalPixelPosition.Position;
        if (_dragging)
        {
            Dragged?.Invoke(_last, pos);
            _last = pos;
        }

        Hovered?.Invoke(pos);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _dragging = false;
        HoverLost?.Invoke();
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        Wheel?.Invoke(args.Delta.Y);
        args.Handle();
    }
}
