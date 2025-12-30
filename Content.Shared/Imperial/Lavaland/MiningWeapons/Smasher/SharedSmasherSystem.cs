using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Timing;
using Content.Shared.Alert;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher;

public abstract partial class SharedSmasherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public bool CanActivateShield(SmasherComponent component)
    {
        return _timing.CurTime >= component.NextActivationTime;
    }

    public void SetCooldown(EntityUid smasherUid, SmasherComponent component, TimeSpan cooldown)
    {
        component.NextActivationTime = _timing.CurTime + cooldown;
        Dirty(smasherUid, component);
    }

    /// <summary>
    /// Updates the cooldown alert display based on remaining cooldown time
    /// </summary>
    public void UpdateCooldownAlert(EntityUid user, SmasherComponent component)
    {
        if (!user.Valid)
        {
            _alerts.ClearAlert(user, component.CounterCooldownAlert);
            return;
        }

        var remainingCooldown = component.NextActivationTime - _timing.CurTime;
        if (remainingCooldown < TimeSpan.Zero)
            remainingCooldown = TimeSpan.Zero;

        var secondsRemaining = (int)Math.Ceiling(remainingCooldown.TotalSeconds);

        // If the cooldown is negative (ended), show 0
        if (secondsRemaining <= 0)
            secondsRemaining = 0;

        var alertSeverity = CalculateAlertSeverity(secondsRemaining);
        _alerts.ShowAlert(user, component.CounterCooldownAlert, (short)alertSeverity);
    }

    /// <summary>
    /// Calculates alert severity based on remaining cooldown seconds
    /// Rounds up to nearest multiple of 5
    /// </summary>
    public int CalculateAlertSeverity(int secondsRemaining)
    {
        var roundedSeconds = (int)Math.Ceiling(secondsRemaining / 5.0) * 5;
        roundedSeconds = Math.Min(roundedSeconds, 60);
        return roundedSeconds / 5;
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

    public bool TryGetHolder(EntityUid smasherUid, [NotNullWhen(true)] out EntityUid? user)
    {
        user = null;

        var query = EntityQueryEnumerator<HandsComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            foreach (var hand in _handsSystem.EnumerateHands(uid))
            {
                if (_handsSystem.TryGetHeldItem(uid, hand, out var heldEntity) && heldEntity == smasherUid)
                {
                    user = uid;
                    return true;
                }
            }
        }

        return false;
    }
}
