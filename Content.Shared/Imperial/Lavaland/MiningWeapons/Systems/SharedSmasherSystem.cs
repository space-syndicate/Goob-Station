using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Systems;

public abstract class SharedSmasherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShieldActivatedEvent>(OnShieldActivated);
        SubscribeNetworkEvent<ShieldChargingEvent>(OnShieldCharging);
        SubscribeNetworkEvent<ShieldChargingEndEvent>(OnShieldChargingEnd);
        SubscribeLocalEvent<ShieldActiveComponent, DamageModifyEvent>(OnDamage);
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

    private void OnDamage(EntityUid uid, ShieldActiveComponent component, DamageModifyEvent args)
    {
        args.Damage *= 0.2f;
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
