using Content.Shared.Actions;

namespace Content.Shared._CorvaxGoob.MindLink;

/// <summary>
/// Enables direct mental messages and stores the user's established recipients.
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

    /// <summary>
    /// Allows keeping several established recipients instead of replacing the previous one.
    /// </summary>
    public bool MultiLink;

    [ViewVariables]
    public float Range = 20f;

    [ViewVariables]
    public HashSet<EntityUid> Targets = new();

    /// <summary>
    /// Recipients selected in the currently open message window.
    /// </summary>
    public List<EntityUid> PendingTargets = new();

    [ViewVariables]
    public EntityUid? PendingReplyTarget;

    [ViewVariables]
    public bool SelectingReplyTarget;
}

/// <summary>
/// Tracks incoming mind links and the subset that allow replies.
/// </summary>
[RegisterComponent]
public sealed partial class MindLinkRecipientComponent : Component
{
    /// <summary>
    /// All entities with an outgoing connection to this recipient.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Initiators = new();

    /// <summary>
    /// Initiators whose connections allow the recipient to reply.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> ReplyInitiators = new();

    [ViewVariables]
    public EntityUid? ReplyAction;
}

public sealed partial class OpenMindLinkEvent : InstantActionEvent
{
    [DataField]
    public bool TwoWayCommunication = true;

    /// <summary>
    /// If true, selecting a new recipient preserves existing links.
    /// </summary>
    [DataField]
    public bool MultiLink;

    /// <summary>
    /// Maximum distance in tiles for establishing a new link.
    /// A negative value allows establishing it at any distance.
    /// </summary>
    [DataField]
    public float Range = 20f;
}

public sealed partial class ReplyMindLinkEvent : InstantActionEvent;
