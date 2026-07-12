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
        SubscribeLocalEvent<NanoChatServerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<NanoChatServerComponent> ent, ref ComponentShutdown args)
    {
        var ev = new NanoChatServerShutdownEvent(ent);
        RaiseLocalEvent(ent, ref ev);
    }
}
