using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.NinjaHeadset.Events;

[Serializable, NetSerializable]
public sealed partial class NinjaHackDoAfterEvent : SimpleDoAfterEvent
{
}
