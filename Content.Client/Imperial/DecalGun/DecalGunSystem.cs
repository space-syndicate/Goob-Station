using Content.Shared.Decals;
using Content.Shared.Imperial.DecalGun;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.DecalGun;

/// <summary>
/// Client-side system responsible for managing the list of decal prototypes available for use in the decal gun UI.
/// It preloads and caches all decal prototypes, and updates the list automatically when decal prototypes are reloaded.
/// Inherits shared logic from <see cref="SharedDecalGunSystem"/>.
/// </summary>
public sealed class DecalGunSystem : SharedDecalGunSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly HashSet<DecalPrototype> _decalListForGun = new();

    public override void Initialize()
    {
        base.Initialize();

        PrepareAllVariants();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReload);
    }

    private void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        PrepareAllVariants();
    }

    /// <summary>
    /// Returns the list of decal prototypes that should be displayed in the decal gun menu.
    /// </summary>
    public HashSet<DecalPrototype> GetDecalForGun()
    {
        return _decalListForGun;
    }

    /// <summary>
    /// Refreshes the internal decal list by loading all prototypes with <c>ShowMenu = true</c>.
    /// </summary>
    private void PrepareAllVariants()
    {
        _decalListForGun.Clear();

        foreach (var prototype in _prototypes.EnumeratePrototypes<DecalPrototype>())
        {
            if (prototype.ShowMenu)
                _decalListForGun.Add(prototype);
        }
    }
}
