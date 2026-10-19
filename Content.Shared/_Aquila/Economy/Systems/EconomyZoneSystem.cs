using Content.Shared._Aquila.CCVars;
using Content.Shared._Aquila.Economy.Components;
using Robust.Shared.Configuration;

namespace Content.Shared._Aquila.Economy.Systems;

public sealed class EconomyZoneSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>
    /// Проверяет, включена ли экономика на сервере.
    /// </summary>
    public bool IsEconomyEnabled()
    {
        return _cfg.GetCVar(AquilaCVars.Economy);
    }

    /// <summary>
    /// Проверяет, действует ли экономика там, где находится сущность: она включена на сервере, а грид и карта не отмечены как зона без экономики.
    /// </summary>
    public bool IsEconomyActive(EntityUid uid)
    {
        if (!IsEconomyEnabled())
            return false;

        var xform = Transform(uid);
        return !HasComp<EconomyFreeZoneComponent>(xform.GridUid) && !HasComp<EconomyFreeZoneComponent>(xform.MapUid);
    }

    /// <summary>
    /// Проверяет, отмечена ли карта или грид как зона без экономики.
    /// </summary>
    public bool IsFreeZone(EntityUid zone)
    {
        return HasComp<EconomyFreeZoneComponent>(zone);
    }

    /// <summary>
    /// Отмечает карту или грид как зону без экономики или снимает отметку.
    /// </summary>
    public void SetFreeZone(EntityUid zone, bool free)
    {
        if (free)
            EnsureComp<EconomyFreeZoneComponent>(zone);
        else
            RemComp<EconomyFreeZoneComponent>(zone);
    }
}
