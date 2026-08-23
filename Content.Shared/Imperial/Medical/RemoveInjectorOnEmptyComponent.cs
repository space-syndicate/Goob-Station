using Content.Shared.Chemistry.Components;

namespace Content.Shared.Imperial.Medical;

/// <summary>
/// Removes <see cref="InjectorComponent"/> when the linked solution is empty,
/// so used single-dose injectors no longer steal interactions (e.g. disposal insert).
/// </summary>
[RegisterComponent]
[Access(typeof(RemoveInjectorOnEmptySystem))]
public sealed partial class RemoveInjectorOnEmptyComponent : Component;
