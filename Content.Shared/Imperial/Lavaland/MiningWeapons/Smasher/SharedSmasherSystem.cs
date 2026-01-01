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

    // Used dictionary because system dictionaries store temporary server-side state;
    // components store permanent networked data.
    private Dictionary<(EntityUid User, EntityUid Smasher), (TimeSpan StartTime, bool Hidden)> _alertZeroData = new();

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
    public void UpdateCooldownAlert(EntityUid smasherUid, EntityUid user, SmasherComponent component)
    {
        if (!user.Valid)
        {
            _alerts.ClearAlert(user, component.CounterCooldownAlert);
            _alertZeroData.Remove((user, smasherUid));
            return;
        }

        var remainingCooldown = component.NextActivationTime - _timing.CurTime;
        if (remainingCooldown < TimeSpan.Zero)
            remainingCooldown = TimeSpan.Zero;

        var secondsRemaining = (int)Math.Ceiling(remainingCooldown.TotalSeconds);
        var key = (user, smasherUid);

        if (secondsRemaining > 0)
        {
            _alertZeroData.Remove(key);
            var alert = CalculateAlertSeverity(secondsRemaining);
            _alerts.ShowAlert(user, component.CounterCooldownAlert, (short)alert);
            return;
        }

        secondsRemaining = 0;

        if (!_alertZeroData.TryGetValue(key, out var data))
        {
            data = (StartTime: _timing.CurTime, Hidden: false);
            _alertZeroData[key] = data;
        }

        if (data.Hidden)
            return;

        if (_timing.CurTime >= data.StartTime + TimeSpan.FromSeconds(5))
        {
            _alerts.ClearAlert(user, component.CounterCooldownAlert);
            data.Hidden = true;
            _alertZeroData[key] = data;
            return;
        }

        var alertSeverity = CalculateAlertSeverity(secondsRemaining);
        _alerts.ShowAlert(user, component.CounterCooldownAlert, (short)alertSeverity);
    }

    /// <summary>
    /// Calculates alert severity based on remaining cooldown seconds
    /// Rounds up to nearest multiple of 5
    /// </summary>
    public int CalculateAlertSeverity(int secondsRemaining)
    {
        if (secondsRemaining <= 0)
            return 0;

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
