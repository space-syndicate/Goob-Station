using System.Linq;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Content.Shared.Examine;
using Content.Server.Administration.Managers;
using Content.Server.Administration;
using Content.Shared.Administration.Logs;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore;

public sealed class CoreCollisionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CoreCollisionComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CoreCollisionComponent, StartCollideEvent>(OnStartCollide);
    }
    private void OnExamined(EntityUid uid, CoreCollisionComponent col, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (col.Deletions)
            args.PushMarkup(Loc.GetString("energycore-was-collision-true"));
        else
            args.PushMarkup(Loc.GetString("energycore-was-collision-false"));

        var count = col.EntitiesDeleted;
        args.PushMarkup(Loc.GetString("energycore-collision-deletions", ("count", count)));
    }
    private void OnStartCollide(EntityUid uid, CoreCollisionComponent col, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (HasComp<CoreShieldingComponent>(target))
            return;

        DeleteEntity(target, col);
        _audio.PlayPvs(col.DelitionSound, uid);
    }

    private void DeleteEntity(EntityUid target, CoreCollisionComponent col)
    {
        col.EntitiesDeleted = col.EntitiesDeleted + 1;
        col.Deletions = true;
        _adminLogger.Add(LogType.Gib,
        LogImpact.Extreme, $"Объект {target} был уничтожен об ядро.");
        Del(target);
    }
}

