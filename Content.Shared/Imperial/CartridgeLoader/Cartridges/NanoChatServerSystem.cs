using Content.Shared.Power;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

/// <summary>
/// This handles...
/// </summary>
public sealed class NanoChatServerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NanoChatServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NanoChatServerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<NanoChatServerComponent> ent, ref ComponentStartup args)
    {
        var ev = new NanoChatServerStartupEvent(ent);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnShutdown(Entity<NanoChatServerComponent> ent, ref ComponentShutdown args)
    {
        var ev = new NanoChatServerShutdownEvent(ent);
        RaiseLocalEvent(ent, ref ev);
    }
}
