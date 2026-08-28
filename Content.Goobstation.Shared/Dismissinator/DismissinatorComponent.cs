// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Dismissinator;

/// <summary>
///     "Увольнятор" — HoP sidearm. Requires an authorized ID card, a sheet of paper and a rubber stamp
///     inserted into it. On hit it strips the victim's ID card and spits out a filled, stamped dismissal notice.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DismissinatorComponent : Component
{
    /// <summary>
    ///     ItemSlot ids, see the ItemSlots component on the prototype.
    /// </summary>
    [DataField]
    public string IdSlotId = "dismissinator-id";

    [DataField]
    public string PaperSlotId = "dismissinator-paper";

    [DataField]
    public string StampSlotId = "dismissinator-stamp";

    /// <summary>
    ///     The inserted ID card must hold this access level, otherwise the gun refuses to fire.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype> RequiredAccess = "HeadOfPersonnel";

    /// <summary>
    ///     Which paperwork the next shot serves. Toggled in hand or through the alt-click verb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DismissinatorMode Mode = DismissinatorMode.Dismissal;

    /// <summary>
    ///     Document spawned at the target on hit, per mode.
    /// </summary>
    [DataField]
    public EntProtoId DismissalDocument = "PaperDismissalNotice";

    [DataField]
    public EntProtoId ExpansionDocument = "PaperAccessExpansionNotice";

    [DataField]
    public EntProtoId ObjectiveDocument = "PaperCovertDirective";

    /// <summary>
    ///     Game rule the emagged mode recruits the target into.
    /// </summary>
    [DataField]
    public EntProtoId TraitorRule = "Traitor";

    /// <summary>
    ///     Visual effect played on the victim on hit.
    /// </summary>
    [DataField]
    public EntProtoId? HitEffect = "EffectEmpPulse";
}
