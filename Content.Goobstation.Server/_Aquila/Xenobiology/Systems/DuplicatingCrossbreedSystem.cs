using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class DuplicatingCrossbreedSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DuplicatingCrossbreedComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DuplicatingCrossbreedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DuplicatingCrossbreedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("slime-crossbreed-duplicating-uses", ("uses", ent.Comp.Uses), ("copies", ent.Comp.Copies)));
    }

    private void OnAfterInteract(Entity<DuplicatingCrossbreedComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist)
            || MetaData(target).EntityPrototype is not { } proto)
            return;

        args.Handled = true;

        for (var i = 0; i < ent.Comp.Copies; i++)
        {
            SpawnNextToOrDrop(proto.ID, target);
        }

        if (ent.Comp.Effect is { } effect)
            SpawnAtPosition(effect, Transform(target).Coordinates);

        _audio.PlayPvs(ent.Comp.Sound, target);
        _popup.PopupEntity(Loc.GetString("slime-crossbreed-duplicating-used", ("target", target)), target, args.User, PopupType.Medium);
        _adminLog.Add(LogType.EntityEffect, LogImpact.Low, $"{ToPrettyString(args.User):user} duplicated {ToPrettyString(target):target} with {ToPrettyString(ent):item}");

        ent.Comp.Uses--;
        if (ent.Comp.Uses <= 0)
            QueueDel(ent);
    }
}
