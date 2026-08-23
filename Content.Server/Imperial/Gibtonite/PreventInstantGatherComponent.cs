namespace Content.Server.Imperial.Gibtonite;

/// <summary>
/// Blocks DestroyEntity so Gatherable cannot one-shot delete the entity.
/// Gibtonite must take damage and prime its fuse instead.
/// </summary>
[RegisterComponent]
[Access(typeof(GibtoniteGatherProtectionSystem))]
public sealed partial class PreventInstantGatherComponent : Component;
