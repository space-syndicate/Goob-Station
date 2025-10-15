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
    [Dependency] protected readonly IGameTiming Timing = default!;
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

        var query = EntityQueryEnumerator<ShieldActiveComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shield, out var xform))
        {
            if (!TryComp<SmasherComponent>(shield.ShieldUid, out var smasher))
                continue;

            if (!TryComp<ShieldActiveComponent>(shield.ShieldUid, out var shieldActive))
                continue;

            if (Timing.CurTime >= shieldActive.EndTime)
            {
                Log.Debug($"Щит для {ToPrettyString(uid)} деактивирован по времени");
                DeactivateShield(uid, shield.ShieldUid);
            }
        }
    }

    private void OnDamage(EntityUid uid, ShieldActiveComponent component, DamageModifyEvent args)
    {
        // TODO: change this
        args.Damage *= 0f;
        Log.Info($"ентити для отражения дамага: {ToPrettyString(uid)}");
    }

    private void OnShieldActivated(ShieldActivatedEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryGetEntity(ev.Smasher, out var smasherUid))
            return;

        if (!TryComp<SmasherComponent>(smasherUid, out var smasher))
            return;

        if (!CanActivateShield(smasherUid.Value, smasher))
        {
            Log.Debug($"Щит еще на кулдауне для {ToPrettyString(user.Value)}");
            return;
        }

        if (HasComp<ShieldActiveComponent>(user.Value))
        {
            Log.Debug($"Щит уже активирован для {ToPrettyString(user.Value)}");
            return;
        }

        Log.Debug($"Активируем щит для {ToPrettyString(user.Value)}");
        ActivateShield(smasherUid.Value, smasher, user.Value);
    }

    public void ActivateShield(EntityUid smasherUid, SmasherComponent smasher, EntityUid user)
    {
        var shieldActive = AddComp<ShieldActiveComponent>(user);

        shieldActive.Effect = smasher.Effect;
        shieldActive.ShieldUid = smasherUid;
        shieldActive.EndTime = Timing.CurTime + TimeSpan.FromSeconds(5);
        Dirty(user, shieldActive);

        SetCooldown(smasherUid, smasher, TimeSpan.FromSeconds(10));

        _audio.PlayPvs(smasher.ActivateSound, user);
    }

    private void DeactivateShield(EntityUid user, EntityUid shieldUid)
    {
        if (!TryComp<SmasherComponent>(shieldUid, out var smasher))
            return;

        RemComp<ShieldActiveComponent>(user);
        _audio.PlayPvs(smasher.DeactivateSound, user);
    }

    public bool CanActivateShield(EntityUid smasherUid, SmasherComponent component)
    {
        return Timing.CurTime >= component.NextActivationTime;
    }

    public void SetCooldown(EntityUid smasherUid, SmasherComponent component, TimeSpan cooldown)
    {
        component.NextActivationTime = Timing.CurTime + cooldown;
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
