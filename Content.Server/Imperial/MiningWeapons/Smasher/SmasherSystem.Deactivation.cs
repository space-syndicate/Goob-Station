using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.MiningWeapons.Smasher.Components;
using Robust.Shared.Input;

namespace Content.Server.Imperial.MiningWeapons.Smasher;

public sealed partial class SmasherSystem
{
    #region Shield Deactivation
    private void CancelCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        smasher.ActiveCharges.Remove(smasherUid);

        if (smasher.EffectDecay != null)
        {
            ShowShieldEffect(user, smasher.EffectDecay, false);
        }

        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }

        var decay = EnsureComp<ShieldDecayComponent>(user);
        decay.DecayEndTime = _timing.CurTime + smasher.TimeDecay;

        smasher.StateUseKey = BoundKeyState.Up;
        _audio.PlayPvs(smasher.DeactivateSound, user);
        SetCooldown(smasherUid, smasher, smasher.TimeCooldownDownedDecay);
    }

    private void DeactivateShield(EntityUid user, ShieldActiveComponent shield)
    {
        if (!TryComp<SmasherComponent>(shield.SmasherUid, out var smasher))
            return;

        HideShieldEffect(user);

        smasher.StateUseKey = BoundKeyState.Up;
        _audio.PlayPvs(smasher.DeactivateSound, user);
        RemComp<ShieldActiveComponent>(user);
    }

    private bool CheckDamageInterruption(EntityUid user, SmasherComponent smasher)
    {
        if (!TryComp<DamageableComponent>(user, out var damageComp))
            return false;

        if (!smasher.LastTotalDamage.TryGetValue(user, out var lastDamage))
        {
            smasher.LastTotalDamage[user] = damageComp.TotalDamage;
            return false;
        }

        var damageReceived = damageComp.TotalDamage - lastDamage;

        if (damageReceived > FixedPoint2.New(1.0))
        {
            if (smasher.EffectDecay != null)
            {
                ShowShieldEffect(user, smasher.EffectDecay, false);
            }

            if (HasComp<SmasherChargingComponent>(user))
            {
                RemComp<SmasherChargingComponent>(user);
                _movementSpeed.RefreshMovementSpeedModifiers(user);
            }

            smasher.LastTotalDamage.Remove(user);
            return true;
        }

        smasher.LastTotalDamage[user] = damageComp.TotalDamage;
        return false;
    }
    #endregion
}
