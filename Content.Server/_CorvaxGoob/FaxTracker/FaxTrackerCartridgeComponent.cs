using Content.Shared._CorvaxGoob.FaxTracker;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.FaxTracker;

[RegisterComponent]
public sealed partial class FaxTrackerCartridgeComponent : Component
{
    [DataField]
    public EntityUid? BoundFax;

    [DataField]
    public bool NotificationsOn = true;

    [ViewVariables]
    public List<FaxTrackerEntry> History = new List<FaxTrackerEntry>();

    [ViewVariables]
    public int ReceivedCount;

    [DataField]
    public int MaxHistory = 50;

    [DataField]
    public Dictionary<string, string> BlacklistedSenders = new Dictionary<string, string>();

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, List<string>> JobFaxNames = new Dictionary<ProtoId<JobPrototype>, List<string>>();
}
