using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics;

[Serializable, NetSerializable]
public sealed partial class GeneInsertingDoAfterEvent : SimpleDoAfterEvent
{
}
[Serializable, NetSerializable]
public sealed partial class GeneWithdrawDoAfterEvent : SimpleDoAfterEvent
{
}