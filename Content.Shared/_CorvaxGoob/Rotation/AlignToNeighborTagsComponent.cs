// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Rotation;

/// <summary>
/// Rotates an entity to match neighboring anchored entities with the configured tags.
/// Useful for construction intermediates that should visually line up with nearby walls.
/// </summary>
[RegisterComponent]
public sealed partial class AlignToNeighborTagsComponent : Component
{
    /// <summary>
    /// Neighbor tags that count as valid alignment anchors.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype>[] Tags = [];

    /// <summary>
    /// Rotation used when matching east/west neighbors.
    /// </summary>
    [DataField]
    public Angle HorizontalRotation = Angle.Zero;

    /// <summary>
    /// Rotation used when matching north/south neighbors.
    /// </summary>
    [DataField]
    public Angle VerticalRotation = Angle.FromDegrees(90);

    /// <summary>
    /// If true, both this entity and matching neighbors must be anchored.
    /// </summary>
    [DataField]
    public bool RequireAnchored = true;
}
