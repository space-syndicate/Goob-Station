namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[ByRefEvent]
public readonly record struct NanoChatServerShutdownEvent(Entity<NanoChatServerComponent> Server);
