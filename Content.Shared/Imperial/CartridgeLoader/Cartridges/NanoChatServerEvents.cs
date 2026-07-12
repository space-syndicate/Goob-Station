namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[ByRefEvent]
public readonly record struct NanoChatServerShutdownEvent(Entity<NanoChatServerComponent> Server);

[ByRefEvent]
public readonly record struct NanoChatServerStartupEvent(Entity<NanoChatServerComponent> Server);
