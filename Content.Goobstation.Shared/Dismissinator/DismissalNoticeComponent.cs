// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Dismissinator;

/// <summary>
///     Attached to a projectile fired from a <see cref="DismissinatorComponent"/>.
///     Carries everything the projectile needs on impact, so the gun itself may be dropped,
///     emptied or destroyed while the bolt is still in flight.
/// </summary>
[RegisterComponent]
public sealed partial class DismissalNoticeComponent : Component
{
    /// <summary>
    ///     Access tags the authorizing ID card held at the moment of the shot. The target can only be
    ///     dismissed if everything on their card falls within this set.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> AuthorizedAccess = new();

    [DataField]
    public StampDisplayInfo Stamp;

    [DataField]
    public string StampState = "paper_stamp-generic";

    /// <summary>
    ///     Which paperwork this bolt is carrying, fixed at the moment of the shot.
    /// </summary>
    [DataField]
    public DismissinatorMode Mode = DismissinatorMode.Dismissal;

    [DataField]
    public EntProtoId Document = "PaperDismissalNotice";

    /// <summary>
    ///     Game rule the target is recruited into by <see cref="DismissinatorMode.Objective"/>.
    /// </summary>
    [DataField]
    public EntProtoId TraitorRule = "Traitor";

    [DataField]
    public EntProtoId? HitEffect = "EffectEmpPulse";

    /// <summary>
    ///     Full name / job title taken from the authorizing ID card, used to fill in the notice.
    /// </summary>
    [DataField]
    public string AuthorName = string.Empty;

    [DataField]
    public string AuthorJob = string.Empty;
}
