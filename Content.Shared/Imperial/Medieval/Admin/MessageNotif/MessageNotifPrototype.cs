using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Admin;

[Prototype]
public sealed partial class MessageNotifPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    [ViewVariables, DataField(required: true)]
    public List<string> Notif = [];
    [DataField(required: false)]
    public bool IsSubword = true;
}
