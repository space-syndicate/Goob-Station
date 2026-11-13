// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Construction;
using Content.Shared._CorvaxGoob.Rotation;
using Content.Shared.Tag;
using Robust.Shared.Map;

namespace Content.Server._CorvaxGoob.Rotation;

/// <summary>
/// Aligns an entity's rotation to nearby anchored entities that have any of the configured tags.
/// </summary>
/// <remarks>
/// The system chooses the horizontal or vertical rotation only when neighbors clearly exist on one axis.
/// If both axes match, or no matching neighbors are found, the current rotation is preserved.
/// </remarks>
public sealed class AlignToNeighborTagsSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Re-evaluate alignment whenever placement, anchoring, or construction replacement can change the visual fit.
        SubscribeLocalEvent<AlignToNeighborTagsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AlignToNeighborTagsComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<AlignToNeighborTagsComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<AlignToNeighborTagsComponent, AfterConstructionChangeEntityEvent>(OnAfterConstructionChange);
    }

    private void OnMapInit(EntityUid uid, AlignToNeighborTagsComponent component, MapInitEvent args)
    {
        Align(uid, component);
    }

    private void OnMove(EntityUid uid, AlignToNeighborTagsComponent component, ref MoveEvent args)
    {
        Align(uid, component, args.Component);
    }

    private void OnAnchorChanged(EntityUid uid, AlignToNeighborTagsComponent component, ref AnchorStateChangedEvent args)
    {
        Align(uid, component, args.Transform);
    }

    private void OnAfterConstructionChange(EntityUid uid, AlignToNeighborTagsComponent component, ref AfterConstructionChangeEntityEvent args)
    {
        Align(uid, component);
    }

    private void Align(EntityUid uid, AlignToNeighborTagsComponent component, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref transform))
            return;

        if (component.RequireAnchored && !transform.Anchored)
            return;

        if (component.Tags.Length == 0)
            return;

        var vertical = HasTaggedNeighbor(uid, transform, new Vector2(0, 1), component)
            || HasTaggedNeighbor(uid, transform, new Vector2(0, -1), component);
        var horizontal = HasTaggedNeighbor(uid, transform, new Vector2(1, 0), component)
            || HasTaggedNeighbor(uid, transform, new Vector2(-1, 0), component);

        // Keep the existing rotation when there is no clear axis, such as corners or isolated entities.
        if (vertical == horizontal)
            return;

        var rotation = vertical
            ? component.VerticalRotation
            : component.HorizontalRotation;

        if (transform.LocalRotation.Equals(rotation))
            return;

        _transform.SetLocalRotation(uid, rotation, transform);
    }

    private bool HasTaggedNeighbor(
        EntityUid uid,
        TransformComponent transform,
        Vector2 offset,
        AlignToNeighborTagsComponent component)
    {
        var coordinates = transform.Coordinates.Offset(offset);

        // A tiny lookup around the neighboring tile center is enough for anchored full-tile structures.
        foreach (var entity in _lookup.GetEntitiesInRange(coordinates, 0.1f, LookupFlags.StaticSundries))
        {
            if (entity == uid)
                continue;

            if (!_tag.HasAnyTag(entity, component.Tags))
                continue;

            if (component.RequireAnchored
                && (!TryComp(entity, out TransformComponent? neighborTransform) || !neighborTransform.Anchored))
                continue;

            return true;
        }

        return false;
    }
}
