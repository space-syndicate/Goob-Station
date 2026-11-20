using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Weapons.Ranged.Systems;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared._BlackM.CCVar;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Mech.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._BlackM.Weapons;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Weapons.Crosshair;

public sealed class GunCrosshairSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    private const float CooldownThreshold = 0.35f;
    private const float FadeSpeed = 9f;
    private const float SpreadSharpness = 18f;
    private const float ColorSharpness = 14f;
    private const float KickDecay = 6f;

    public static readonly Color NormalColor = new(0.96f, 0.97f, 0.98f, 0.92f);
    public static readonly Color TargetColor = new(1f, 0.38f, 0.34f, 0.95f);
    public static readonly Color BlockedColor = new(1f, 0.76f, 0.32f, 0.9f);
    public static readonly Color EmptyColor = new(0.58f, 0.6f, 0.64f, 0.8f);

    private bool _crosshairEnabled;
    private bool _aimLineEnabled;

    private EntityUid? _lastGun;
    private TimeSpan _lastNextFire;
    private int _lastAmmo;
    private float _cooldownTotal;

    public bool Active { get; private set; }
    public float Alpha { get; private set; }
    public bool DrawsCrosshair => Active && _crosshairEnabled;
    public bool CrosshairVisible => _crosshairEnabled && Alpha > 0.01f;
    public bool AimLineVisible => _aimLineEnabled && Alpha > 0.01f;

    public MapId Map { get; private set; }
    public Vector2 Origin { get; private set; }
    public Vector2 Target { get; private set; }
    public float Spread { get; private set; }
    public float Kick { get; private set; }
    public float CooldownProgress { get; private set; } = 1f;
    public bool ShowCooldown { get; private set; }
    public int Ammo { get; private set; }
    public int Capacity { get; private set; }
    public bool OnTarget { get; private set; }
    public bool Blocked { get; private set; }
    public bool Empty => Capacity > 0 && Ammo <= 0;
    public Color CurrentColor { get; private set; } = NormalColor;

    private Color TargetStateColor =>
        Empty ? EmptyColor :
        Blocked ? BlockedColor :
        OnTarget ? TargetColor :
        NormalColor;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, BlackMCVars.GunCrosshairEnabled, OnCrosshairChanged, true);
        Subs.CVar(_cfg, BlackMCVars.GunAimLineEnabled, OnAimLineChanged, true);
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<GunCrosshairOverlay>();
        _overlay.RemoveOverlay<GunAimOverlay>();

        base.Shutdown();
    }

    private void OnCrosshairChanged(bool value)
    {
        _crosshairEnabled = value;

        if (value && !_overlay.HasOverlay<GunCrosshairOverlay>())
            _overlay.AddOverlay(new GunCrosshairOverlay(_eye, _input, _cache, this));
        else if (!value)
            _overlay.RemoveOverlay<GunCrosshairOverlay>();
    }

    private void OnAimLineChanged(bool value)
    {
        _aimLineEnabled = value;

        if (value && !_overlay.HasOverlay<GunAimOverlay>())
            _overlay.AddOverlay(new GunAimOverlay(_eye, this));
        else if (!value)
            _overlay.RemoveOverlay<GunAimOverlay>();
    }

    // Same gun lookup as client GunSystem.Update, mech aware.
    public bool TryGetAiming(EntityUid user, out EntityUid shooter, out EntityUid gunUid, [NotNullWhen(true)] out GunComponent? gun)
    {
        shooter = user;
        gunUid = default;
        gun = null;

        if (!TryComp<CombatModeComponent>(user, out var combat) || !combat.IsInCombatMode)
            return false;

        if (_mobState.IsIncapacitated(user))
            return false;

        if (TryComp<MechPilotComponent>(user, out var pilot))
            shooter = pilot.Mech;

        return _gun.TryGetGun(shooter, out gunUid, out gun);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        Active = (_crosshairEnabled || _aimLineEnabled) && TryUpdate(frameTime);

        if (!Active)
            _lastGun = null;

        var fade = frameTime * FadeSpeed;
        Alpha = Active ? MathF.Min(1f, Alpha + fade) : MathF.Max(0f, Alpha - fade);
        CurrentColor = Color.InterpolateBetween(CurrentColor, TargetStateColor, 1f - MathF.Exp(-ColorSharpness * frameTime));
    }

    private bool TryUpdate(float frameTime)
    {
        if (_player.LocalEntity is not { } user ||
            !TryGetAiming(user, out var shooter, out var gunUid, out var gun))
            return false;

        var mouseScreen = _input.MouseScreenPosition;
        if (!mouseScreen.IsValid || !ReferenceEquals(_ui.CurrentlyHovered, _eye.MainViewport))
            return false;

        var mouse = _eye.PixelToMap(mouseScreen);
        var origin = _xform.GetMapCoordinates(shooter);
        if (mouse.MapId == MapId.Nullspace || mouse.MapId != origin.MapId)
            return false;

        Map = origin.MapId;
        Origin = origin.Position;
        Target = mouse.Position;

        var curTime = _timing.CurTime;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(gunUid, ref ammoEv);
        Ammo = ammoEv.Count;
        Capacity = ammoEv.Capacity;

        if (_lastGun != gunUid)
        {
            _lastGun = gunUid;
            _lastNextFire = gun.NextFire;
            _cooldownTotal = 0f;
            Kick = 0f;
            Spread = GetNextShotSpread(gun, shooter, gunUid, curTime);
        }
        else if (gun.NextFire > _lastNextFire)
        {
            // NextFire also moves on empty clicks and melee, so require spent ammo.
            if (Capacity == 0 || Ammo < _lastAmmo)
            {
                _cooldownTotal = (float) (gun.NextFire - curTime).TotalSeconds;
                Kick = 1f;
            }

            _lastNextFire = gun.NextFire;
        }

        _lastAmmo = Ammo;

        var spread = GetNextShotSpread(gun, shooter, gunUid, curTime);
        Spread += (spread - Spread) * (1f - MathF.Exp(-SpreadSharpness * frameTime));
        Kick = MathF.Max(0f, Kick - frameTime * KickDecay);

        var remaining = (float) (gun.NextFire - curTime).TotalSeconds;
        ShowCooldown = remaining > 0f && _cooldownTotal >= CooldownThreshold;
        CooldownProgress = ShowCooldown ? Math.Clamp(1f - remaining / _cooldownTotal, 0f, 1f) : 1f;

        Blocked = !_actionBlocker.CanAttack(shooter) ||
                  TryComp<ChamberMagazineAmmoProviderComponent>(gunUid, out var chamber) && chamber.BoltClosed == false ||
                  HasComp<GunRequiresWieldComponent>(gunUid) &&
                  TryComp<WieldableComponent>(gunUid, out var wieldable) && !wieldable.Wielded;

        OnTarget = IsAimingAtMob(user, shooter, mouse);
        return true;
    }

    // Mirrors server GetRecoilAngle: half-angle of the next shot cone.
    private float GetNextShotSpread(GunComponent gun, EntityUid shooter, EntityUid gunUid, TimeSpan curTime)
    {
        var theta = BlackMGunSpreadMath.GetNextShotTheta(gun, curTime);

        var ev = new GetRecoilModifiersEvent { Gun = gunUid, User = shooter };
        RaiseLocalEvent(shooter, ev);
        RaiseLocalEvent(gunUid, ev);

        var oneHanded = TryComp<WieldableComponent>(gunUid, out var grip) && !grip.Wielded;
        return (float) Math.Abs(theta * 0.5 * ev.Modifier * BlackMGunSpreadMath.GetGripMultiplier(oneHanded));
    }

    private bool IsAimingAtMob(EntityUid user, EntityUid shooter, MapCoordinates mouse)
    {
        if (_state.CurrentState is not GameplayStateBase screen)
            return false;

        foreach (var ent in screen.GetClickableEntities(mouse))
        {
            if (ent == user || ent == shooter)
                continue;

            if (TryComp<MobStateComponent>(ent, out var mob) && !_mobState.IsDead(ent, mob))
                return true;
        }

        return false;
    }
}
