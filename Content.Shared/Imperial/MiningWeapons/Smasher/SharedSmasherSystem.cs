using Content.Shared.Imperial.MiningWeapons.Smasher.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Timing;
using Content.Shared.Alert;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Imperial.MiningWeapons.Smasher.Events;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.MiningWeapons.Smasher;

public abstract partial class SharedSmasherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected void ShowShieldEffect(EntityUid user, SpriteSpecifier? effect, bool loop)
    {
        if (effect == null) return;
        RaiseNetworkEvent(new ShowShieldEffectEvent(GetNetEntity(user), effect, loop));
    }

    protected void HideShieldEffect(EntityUid user)
    {
        RaiseNetworkEvent(new HideShieldEffectEvent(GetNetEntity(user)));
    }

    protected bool CanActivateShield(SmasherComponent component)
    {
        return _timing.CurTime >= component.NextActivationTime;
    }

    protected void CleanupAlerts(EntityUid smasherUid, SmasherComponent smasher)
    {
        if (smasher.ActiveCharges.TryGetValue(smasherUid, out var chargeData))
        {
            _alerts.ClearAlert(chargeData.User, smasher.CounterCooldownAlert);
        }

        if (smasher.LastAlertedUser.TryGetValue(smasherUid, out var lastUser))
        {
            _alerts.ClearAlert(lastUser, smasher.CounterCooldownAlert);
        }
    }

    protected void SetCooldown(EntityUid smasherUid, SmasherComponent component, TimeSpan cooldown)
    {
        component.NextActivationTime = _timing.CurTime + cooldown;
        Dirty(smasherUid, component);
    }

    /// <summary>
    /// Updates the cooldown alert display based on remaining cooldown time
    /// </summary>
    protected void UpdateCooldownAlert(EntityUid smasherUid, EntityUid user, SmasherComponent component)
    {
        if (!user.Valid)
        {
            _alerts.ClearAlert(user, component.CounterCooldownAlert);
            component.AlertZeroData.Remove((user, smasherUid));
            return;
        }

        var remainingCooldown = component.NextActivationTime - _timing.CurTime;
        if (remainingCooldown < TimeSpan.Zero)
            remainingCooldown = TimeSpan.Zero;

        var secondsRemaining = (int)Math.Ceiling(remainingCooldown.TotalSeconds);
        var key = (user, smasherUid);

        if (secondsRemaining > 0)
        {
            component.AlertZeroData.Remove(key);
            var alert = CalculateAlertSeverity(secondsRemaining);
            _alerts.ShowAlert(user, component.CounterCooldownAlert, (short)alert);
            return;
        }

        secondsRemaining = 0;

        if (!component.AlertZeroData.TryGetValue(key, out var data))
        {
            data = (StartTime: _timing.CurTime, Hidden: false);
            component.AlertZeroData[key] = data;
        }

        if (data.Hidden)
            return;

        if (_timing.CurTime >= data.StartTime + component.TimeDeleteAlert)
        {
            _alerts.ClearAlert(user, component.CounterCooldownAlert);
            data.Hidden = true;
            component.AlertZeroData[key] = data;
            return;
        }

        var alertSeverity = CalculateAlertSeverity(secondsRemaining);
        _alerts.ShowAlert(user, component.CounterCooldownAlert, (short)alertSeverity);
    }

    /// <summary>
    /// Calculates alert severity based on remaining cooldown seconds
    /// Rounds up to nearest multiple of 5
    /// </summary>
    protected int CalculateAlertSeverity(int secondsRemaining)
    {
        if (secondsRemaining <= 0)
            return 0;

        var roundedSeconds = (int)Math.Ceiling(secondsRemaining / 5.0) * 5;
        roundedSeconds = Math.Min(roundedSeconds, 60);
        return roundedSeconds / 5;
    }

    protected bool TryGetSmasherInHands(EntityUid user, [NotNullWhen(true)] out EntityUid? smasherUid, [NotNullWhen(true)] out SmasherComponent? smasherComp)
    {
        smasherUid = null;
        smasherComp = null;

        if (!HasComp<HandsComponent>(user))
            return false;

        foreach (var heldEntity in _handsSystem.EnumerateHeld(user))
        {
            if (TryComp(heldEntity, out smasherComp))
            {
                smasherUid = heldEntity;
                return true;
            }
        }

        return false;
    }

    protected bool TryGetHolder(EntityUid smasherUid, [NotNullWhen(true)] out EntityUid? user)
    {
        user = null;

        var current = smasherUid;
        while (_containerSystem.TryGetContainingContainer(current, out var container))
        {
            if (HasComp<HandsComponent>(container.Owner))
            {
                user = container.Owner;
                return true;
            }

            current = container.Owner;
        }

        return false;
    }
}
