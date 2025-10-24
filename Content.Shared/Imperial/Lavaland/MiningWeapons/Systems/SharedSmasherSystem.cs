using Content.Shared.Imperial.Lavaland.MiningWeapons.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Systems;

public abstract class SharedSmasherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShieldActivatedEvent>(OnShieldActivated);
        SubscribeLocalEvent<ShieldActiveComponent, DamageModifyEvent>(OnDamage);
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

                Log.Debug($"Щит для {ToPrettyString(uid)} деактивирован по времени");
                RemComp<ShieldActiveComponent>(uid);
            }
        }
    }

    private void OnShieldActivated(ShieldActivatedEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null || !TryGetEntity(ev.Smasher, out var smasherUid) || !TryComp<SmasherComponent>(smasherUid, out var smasher))
            return;

        if (!CanActivateShield(smasher) || HasComp<ShieldActiveComponent>(user.Value))
            return;

        Log.Debug($"Активируем щит для {ToPrettyString(user.Value)}");
        ActivateShield(smasherUid.Value, smasher, user.Value);
    }

    private void OnDamage(EntityUid uid, ShieldActiveComponent component, DamageModifyEvent args)
    {
        args.Damage *= 0.2f;
        Log.Info($"Щит поглощает урон для {ToPrettyString(uid)}");
    }

    public void ActivateShield(EntityUid smasherUid, SmasherComponent smasher, EntityUid user)
    {
        var shieldActive = AddComp<ShieldActiveComponent>(user);

        shieldActive.EffectActived = smasher.EffectActived;
        shieldActive.SmasherUid = smasherUid;
        shieldActive.EndTime = _timing.CurTime + TimeSpan.FromSeconds(5);
        Dirty(user, shieldActive);

        SetCooldown(smasherUid, smasher, TimeSpan.FromSeconds(10));

        _audio.PlayPvs(smasher.ActivateSound, user);

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
