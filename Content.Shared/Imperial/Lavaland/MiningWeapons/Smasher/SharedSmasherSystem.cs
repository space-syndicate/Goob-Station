using Content.Shared.Imperial.Damage;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher;

public abstract class SharedSmasherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShieldActivatedEvent>(OnShieldActivated);
        SubscribeNetworkEvent<ShieldChargingEvent>(OnShieldCharging);
        SubscribeNetworkEvent<ShieldChargingEndEvent>(OnShieldChargingEnd);

        SubscribeLocalEvent<ShieldActiveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShieldActiveComponent, ComponentShutdown>(OnShieldShutdown);
        SubscribeLocalEvent<SmasherChargingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShieldActiveComponent>();
        while (query.MoveNext(out var uid, out var shield))
        {
            if (_timing.CurTime >= shield.EndTime)
            {
                if (shield.EffectDecay != null)
                    RaiseNetworkEvent(new ShieldDecayEvent(GetNetEntity(uid), shield.EffectDecay));

                RemComp<ShieldActiveComponent>(uid);
            }
        }
    }

    private void OnShieldCharging(ShieldChargingEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (!user.HasValue)
            return;

        if (!HasComp<SmasherChargingComponent>(user.Value))
        {
            EnsureComp<SmasherChargingComponent>(user.Value);
            _movementSpeed.RefreshMovementSpeedModifiers(user.Value);
        }
    }

    private void OnShieldChargingEnd(ShieldChargingEndEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user != null && HasComp<SmasherChargingComponent>(user.Value))
        {
            RemComp<SmasherChargingComponent>(user.Value);
            _movementSpeed.RefreshMovementSpeedModifiers(user.Value);
        }
    }

    private void OnShieldActivated(ShieldActivatedEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null || !TryGetEntity(ev.Smasher, out var smasherUid) || !TryComp<SmasherComponent>(smasherUid, out var smasher))
            return;

        if (!CanActivateShield(smasher) || HasComp<ShieldActiveComponent>(user.Value))
            return;

        ActivateShield(smasherUid.Value, smasher, user.Value);
    }

    private void OnRefreshMovespeed(EntityUid uid, SmasherChargingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnComponentInit(EntityUid uid, ShieldActiveComponent component, ComponentInit args)
    {
        Log.Debug("ShieldActiveComponent OnComponentInit");
        var blocking = EnsureComp<ImperialShieldComponent>(uid);
        blocking.PassiveBlockDamageModifer ??= new DamageModifierSet();
        blocking.PassiveBlockDamageModifer.Coefficients ??= new Dictionary<string, float>();

        blocking.PassiveBlockDamageModifer.Coefficients = component.DamageBlockedCoefficients;

        blocking.HasBlockSound = true;
    }

    private void OnShieldShutdown(EntityUid uid, ShieldActiveComponent component, ComponentShutdown args)
    {
        if (HasComp<ImperialShieldComponent>(uid))
            RemComp<ImperialShieldComponent>(uid);
        Log.Debug("ShieldActiveComponent OnShieldShutdown");

        if (component.EffectDecay != null)
        {
            RaiseNetworkEvent(new ShowShieldEffectEvent(
                GetNetEntity(uid),
                component.EffectDecay
            ));

            _isDecayEffectActive = true;
            _decayEndTime = _timing.CurTime + _timeDecay;
        }
        else
        {
            RaiseNetworkEvent(new HideShieldEffectEvent(
                GetNetEntity(uid)
            ));
        }

        _audio.PlayPvs(component.DeactivateSound, uid);

        _isHolding = false;
        _isChargingEffectActive = false;
    }

    public void ActivateShield(EntityUid smasherUid, SmasherComponent smasher, EntityUid user)
    {
        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }

        var shieldActive = AddComp<ShieldActiveComponent>(user);

        shieldActive.EffectActived = smasher.EffectActived;
        shieldActive.SmasherUid = smasherUid;
        shieldActive.EndTime = _timing.CurTime + TimeSpan.FromSeconds(5);
        Dirty(user, shieldActive);

        SetCooldown(smasherUid, smasher, TimeSpan.FromSeconds(10));

        RaiseNetworkEvent(new ShieldActivatedEvent(GetNetEntity(smasherUid), GetNetEntity(user),
            smasher.EffectActived, smasher.EffectCharging, smasher.EffectDecay));
    }

    public bool CanActivateShield(SmasherComponent component)
    {
        return _timing.CurTime >= component.NextActivationTime;
    }

    public void SetCooldown(EntityUid smasherUid, SmasherComponent component, TimeSpan cooldown)
    {
        component.NextActivationTime = _timing.CurTime + cooldown;
        Dirty(smasherUid, component);
    }

    public bool TryGetSmasherInHands(EntityUid user, [NotNullWhen(true)] out EntityUid? smasherUid, [NotNullWhen(true)] out SmasherComponent? smasherComp)
    {
        smasherUid = null;
        smasherComp = null;

        if (!HasComp<HandsComponent>(user))
            return false;

        foreach (var hand in _handsSystem.EnumerateHands(user))
        {
            if (_handsSystem.TryGetHeldItem(user, hand, out var heldEntity))
            {
                if (TryComp(heldEntity, out smasherComp))
                {
                    smasherUid = heldEntity;
                    return true;
                }
            }
        }

        return false;
    }
}
