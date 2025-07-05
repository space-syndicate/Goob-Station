using Content.Shared.Imperial.DecalGun;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Imperial.DecalGun;

/// <summary>
/// Client-side user interface handler for the Decal Gun.
/// Manages the opening, population, and state synchronization of the decal placement UI.
/// Sends decal configuration changes to the server and applies state updates from the server.
/// </summary>
[UsedImplicitly]
public sealed partial class DecalGunBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private DecalGunWindow? _window;

    public DecalGunBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var decalSystem = _entityManager.System<DecalGunSystem>();

        _window = this.CreateWindow<DecalGunWindow>();
        _window.Populate(decalSystem.GetDecalForGun());

        _window.OnSelectionChanged += (decal, color, snap, rotation, cleanable) =>
        {
            SendMessage(new DecalGunSelectionChangedMessage(decal, color, snap, rotation, cleanable));
        };

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DecalGunUIState s)
            return;

        _window?.SetData(s.DecalId, s.Color, s.Snap, s.Rotation, s.IsCleanable);
    }
}
