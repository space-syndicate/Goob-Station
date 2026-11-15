using Content.Client._Shitcode.Heretic;
using Content.Goobstation.Shared.StatusIcon.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;

namespace Content.Goobstation.Client.StatusIcon.Systems;

public sealed partial class ForceHideIconsSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;

    [SubscribeLocalEvent(after: [typeof(GhoulSystem)])]
    private void OnGetStatusIcons(Entity<ForceHideIconsComponent> ent, ref GetStatusIconsEvent args)
    {
        if (args.Uid != _player.LocalEntity)
            args.StatusIcons.Clear();
    }
}
