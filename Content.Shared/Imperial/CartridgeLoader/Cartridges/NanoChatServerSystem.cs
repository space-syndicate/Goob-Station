namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

/// <summary>
/// Система, отвечающая за сервера НаноЧата.
/// Объявляет события появления и удаления серверов.
/// </summary>
/// <seealso cref="NanoChatCartridgeComponent"/>
/// <seealso cref="NanoChatServerComponent"/>
public sealed class NanoChatServerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NanoChatServerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NanoChatServerComponent, ComponentStartup>(OnStartup);
    }

    private void OnShutdown(Entity<NanoChatServerComponent> ent, ref ComponentShutdown args)
    {
        var ev = new NanoChatServerShutdownEvent(ent);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnStartup(Entity<NanoChatServerComponent> ent, ref ComponentStartup args)
    {
        var ev = new NanoChatServerStartupEvent(ent);
        RaiseLocalEvent(ent, ref ev);
    }
}
