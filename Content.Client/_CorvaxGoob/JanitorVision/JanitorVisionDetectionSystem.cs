// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.JanitorVision;

/// <summary>Selects cleaning targets already present in the client's PVS; never expands server visibility.</summary>
public sealed class JanitorVisionDetectionSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    /// <summary>
    /// Selects only loose trash and puddles on the observer's map and within range.
    /// Deliberately has no line-of-sight check: walls must not block the scanner.
    /// Collected trash, held items, and entities outside the client's PVS are excluded.
    /// </summary>
    public bool TryGetTargetPosition(EntityUid observer, EntityUid target, float range,
        out Vector2 position)
    {
        position = default;
        if (range <= 0f || Deleted(observer) || Deleted(target)
            || !TryComp(observer, out TransformComponent? observerXform)
            || !TryComp(target, out TransformComponent? targetXform)
            || observerXform.MapID == MapId.Nullspace || observerXform.MapID != targetXform.MapID
            || (MetaData(target).Flags & MetaDataFlags.Detached) != 0
            || _containers.IsEntityInContainer(target))
            return false;

        if (HasComp<PuddleComponent>(target))
        {
            if (HasComp<EvaporationComponent>(target))
                return false;
        }
        else if (!_tags.HasTag(target, TrashTag))
            return false;

        position = _transform.GetWorldPosition(targetXform);
        return Vector2.DistanceSquared(_transform.GetWorldPosition(observerXform), position) <= range * range;
    }

    /// <summary>Foot and body prints are cleanable grid decals, not sprite entities.</summary>
    public bool IsFootprintInRange(EntityUid observer, EntityUid grid, Decal decal, float range)
    {
        if (range <= 0f || !decal.Cleanable || Deleted(observer) || Deleted(grid)
            || !TryComp(observer, out TransformComponent? observerXform)
            || !TryComp(grid, out TransformComponent? gridXform)
            || observerXform.MapID == MapId.Nullspace || observerXform.MapID != gridXform.MapID
            || (MetaData(grid).Flags & MetaDataFlags.Detached) != 0
            || !_prototypes.TryIndex<DecalPrototype>(decal.Id, out var prototype)
            || !prototype.Tags.Contains("footprint"))
            return false;

        // Decal coordinates refer to the lower-left corner of the one-tile texture quad.
        var position = Vector2.Transform(decal.Coordinates + new Vector2(0.5f), _transform.GetWorldMatrix(gridXform));
        return Vector2.DistanceSquared(_transform.GetWorldPosition(observerXform), position) <= range * range;
    }
}
