using Content.Shared.Imperial.MiningWeapons.Smasher;
using Content.Shared.Imperial.MiningWeapons.Smasher.Components;

namespace Content.Server.Imperial.MiningWeapons.Smasher;

public sealed partial class SmasherSystem
{
    #region Shield Activation
    private void CompleteCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        smasher.ActiveCharges.Remove(smasherUid);
        ActivateShield(smasherUid, smasher, user);

        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }
    }

    public void ActivateShield(EntityUid smasherUid, SmasherComponent smasher, EntityUid user)
    {
        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }

        if (HasComp<ShieldActiveComponent>(user))
            return;

        var shieldActive = EnsureComp<ShieldActiveComponent>(user);
        shieldActive.EffectActived = smasher.EffectActived;
        shieldActive.SmasherUid = smasherUid;
        shieldActive.EndTime = _timing.CurTime + smasher.ActiveShieldTime;
        Dirty(user, shieldActive);

        SetCooldown(smasherUid, smasher, smasher.ActiveShieldCooldown);
        ShieldActivated(user, smasher);
    }

    private void ShieldActivated(EntityUid user, SmasherComponent smasher)
    {
        ShowShieldEffect(user, smasher.EffectActived, true);
        _audio.PlayPvs(smasher.ActivateSound, user);
    }

    #region Helpers
    private void StartCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        var chargeData = new ChargeData
        {
            StartTime = _timing.CurTime,
            User = user
        };
        smasher.ActiveCharges[smasherUid] = chargeData;

        ShowShieldEffect(user, smasher.EffectCharging, true);
        EnsureComp<SmasherChargingComponent>(user);
        _movementSpeed.RefreshMovementSpeedModifiers(user);
        _audio.PlayPvs(smasher.StartChargingSound, user);
    }
    #endregion
    #endregion
}
