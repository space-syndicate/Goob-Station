using System.Numerics;
using Content.Client.Weapons.Melee;
using Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;
using Content.Shared.CombatMode;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// Свайп-боевка: зажал ЛКМ — курсор замирает (относительный режим мыши), движение мыши копится;
/// отпустил — тип удара выбирается по направлению движения относительно того, куда смотрит персонаж.
/// </summary>
public sealed class SwipeCombatInputSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>Идёт свайп: курсор зафиксирован. Используется, чтобы персонаж не поворачивался за мышью.</summary>
    public bool IsSwiping => _held;

    /// <summary>Где застыл курсор (позиция в момент нажатия ЛКМ).</summary>
    public ScreenCoordinates FrozenCursor => _startScreen;

    private bool _held;
    private EntityUid? _heldWeapon;

    /// <summary>Позиция курсора в момент нажатия — точка отсчёта для перевода сдвига в мировые координаты.</summary>
    private ScreenCoordinates _startScreen;

    /// <summary>Накопленный сдвиг мыши в пикселях экрана.</summary>
    private Vector2 _delta;

    public override void Initialize()
    {
        base.Initialize();
        _clyde.MouseMove += OnMouseMove;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _clyde.MouseMove -= OnMouseMove;
        StopSwipe();
    }

    private void OnMouseMove(MouseMoveEventArgs args)
    {
        if (_held)
            _delta += args.Relative;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user
            || !_combat.IsInCombatMode(user)
            || !_melee.TryGetWeapon(user, out var weaponUid, out var melee)
            || !TryComp<SwipeCombatComponent>(weaponUid, out var swipe))
        {
            StopSwipe();
            return;
        }

        var down = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) == BoundKeyState.Down;

        if (down && !_held)
        {
            _held = true;
            _heldWeapon = weaponUid;
            _startScreen = _input.MouseScreenPosition;
            _delta = Vector2.Zero;
            _clyde.MainWindow.SetRelativeMouseMode(true);
            return;
        }

        if (down || !_held)
            return;

        // ЛКМ отпущена.
        var delta = _delta;
        var heldWeapon = _heldWeapon;
        StopSwipe();


        if (heldWeapon != weaponUid || delta.Length() < swipe.DragThreshold || melee.NextAttack > _timing.CurTime)
            return;

        var userXform = Transform(user);
        var userMap = _transform.GetMapCoordinates(user, userXform);
        if (userMap.MapId == MapId.Nullspace)
            return;

        // Куда смотрит персонаж.
        var forward = _transform.GetWorldRotation(userXform).ToWorldVec();
        var right = new Vector2(forward.Y, -forward.X);

        // Сдвиг мыши в мировых координатах (учитывает поворот и зум камеры).
        var worldDelta = _eye.PixelToMap(new ScreenCoordinates(_startScreen.Position + delta, _startScreen.Window)).Position
                         - _eye.PixelToMap(_startScreen).Position;

        var along = Vector2.Dot(worldDelta, forward);
        var across = Vector2.Dot(worldDelta, right);

        SwipeAttackDirection direction;
        if (MathF.Abs(along) >= MathF.Abs(across))
            direction = along > 0 ? SwipeAttackDirection.Thrust : SwipeAttackDirection.Overhead;
        else
            direction = across > 0 ? SwipeAttackDirection.SlashRight : SwipeAttackDirection.SlashLeft;

        // Удар идёт туда, куда смотрит персонаж.
        var aim = new MapCoordinates(userMap.Position + forward, userMap.MapId);
        var coordinates = _map.TryFindGridAt(aim, out var gridUid, out _)
            ? _transform.ToCoordinates(gridUid, aim)
            : _transform.ToCoordinates(_map.GetMap(aim.MapId), aim);

        RaiseNetworkEvent(new SwipeAttackRequestEvent(GetNetEntity(weaponUid), direction, GetNetCoordinates(coordinates)));
    }

    private void StopSwipe()
    {
        if (!_held)
            return;

        _held = false;
        _heldWeapon = null;
        _delta = Vector2.Zero;
        _clyde.MainWindow.SetRelativeMouseMode(false);
    }
}
