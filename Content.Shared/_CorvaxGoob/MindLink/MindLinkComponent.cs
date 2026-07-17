using Content.Shared.Actions;

namespace Content.Shared._CorvaxGoob.MindLink;

/// <summary>
/// Enables direct mental messages and stores the user's current recipient.
/// </summary>
[RegisterComponent]
public sealed partial class MindLinkComponent : Component
{
    /// <summary>
    /// Whether this component was added only to host the reply UI for a recipient.
    /// </summary>
    public bool TemporaryUiHost;

    /// <summary>
    /// Whether component initialization added the entity's user interface component.
    /// </summary>
    public bool AddedUserInterface;

    /// <summary>
    /// Set from the action prototype when the target picker is opened.
    /// </summary>
    public bool TwoWayCommunication = true;

    [ViewVariables]
    public float Range = 20f;

    [ViewVariables]
    public EntityUid? CurrentTarget;

    [ViewVariables]
    public EntityUid? PendingReplyTarget;

    [ViewVariables]
    public bool SelectingReplyTarget;
}

/// <summary>
/// Added at runtime to a recipient while it can reply through a mind link.
/// </summary>
[RegisterComponent]
public sealed partial class MindLinkRecipientComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Initiators = new();

    [ViewVariables]
    public EntityUid? ReplyAction;
}

public sealed partial class OpenMindLinkEvent : InstantActionEvent
{
    [DataField]
    public bool TwoWayCommunication = true;

    /// <summary>
    /// Maximum distance in tiles for establishing a new link.
    /// A negative value allows establishing it at any distance.
    /// </summary>
    [DataField]
    public float Range = 20f;
}

public sealed partial class ReplyMindLinkEvent : InstantActionEvent;
