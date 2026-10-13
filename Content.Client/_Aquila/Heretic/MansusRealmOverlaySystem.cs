using Content.Shared._Aquila.Heretic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Aquila.Heretic;

public sealed class MansusRealmOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private MansusRealmOverlay _realmOverlay = default!;
    private MansusHandArmOverlay _armOverlay = default!;
    private MansusFadeOverlay _fadeOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _realmOverlay = new MansusRealmOverlay();
        _armOverlay = new MansusHandArmOverlay();
        _fadeOverlay = new MansusFadeOverlay();
        _overlay.AddOverlay(_armOverlay);

        SubscribeLocalEvent<MansusVictimComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MansusVictimComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MansusVictimComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MansusVictimComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_realmOverlay);
        _overlay.RemoveOverlay(_armOverlay);
        _overlay.RemoveOverlay(_fadeOverlay);
    }

    private void OnStartup(Entity<MansusVictimComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.AddOverlay(_realmOverlay);
        _overlay.AddOverlay(_fadeOverlay);
    }

    private void OnShutdown(Entity<MansusVictimComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.RemoveOverlay(_realmOverlay);
        _overlay.RemoveOverlay(_fadeOverlay);
    }

    private void OnPlayerAttached(Entity<MansusVictimComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlay.AddOverlay(_realmOverlay);
        _overlay.AddOverlay(_fadeOverlay);
    }

    private void OnPlayerDetached(Entity<MansusVictimComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_realmOverlay);
        _overlay.RemoveOverlay(_fadeOverlay);
    }
}
