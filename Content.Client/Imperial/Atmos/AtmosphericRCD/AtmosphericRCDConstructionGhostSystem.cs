using Content.Shared.Hands.Components;
using Content.Client.Hands.Systems;
using Content.Shared.Interaction;
using Content.Shared.Imperial.Atmospheric.RCD;
using Content.Shared.Imperial.Atmospheric.RCD.Components;
using Content.Shared.Imperial.Atmospheric.RCD.Systems;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Atmospheric.RCD;

public sealed class AtmosphericRCDConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private string _placementMode = typeof(AtmosphericAlignRCDConstruction).Name;
    private Direction _placementDirection = default;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRCD = HasComp<AtmosphericRCDComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RCD (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRCD))
            return;

        // Determine if player is carrying an RCD in their active hand
        if (_playerManager.LocalSession?.AttachedEntity is not { } player)
            return;

        var heldEntity = _hands.GetActiveItem(player);

        if (heldEntity != null && IsClientSide(heldEntity.Value))
            return;

        if (!TryComp<AtmosphericRCDComponent>(heldEntity, out var rcd))
        {
            // If the player was holding an RCD, but is no longer, cancel placement
            if (placerIsRCD)
                _placementManager.Clear();

            return;
        }
        var prototype = _protoManager.Index(rcd.ProtoId);

        // Update the direction the RCD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new AtmosphericRCDConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        // If the placer has not changed, exit
        if (heldEntity == placerEntity && prototype.Prototype == placerProto)
            return;

        // Create a new placer
        var newObjInfo = new PlacementInformation
        {
            MobUid = heldEntity.Value,
            PlacementOption = _placementMode,
            EntityType = prototype.Prototype,
            Range = (int)Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = (prototype.Mode == AtmosphericRcdMode.ConstructTile),
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
