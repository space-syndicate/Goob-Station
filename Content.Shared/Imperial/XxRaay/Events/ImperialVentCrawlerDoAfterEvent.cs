using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Events;

[Serializable, NetSerializable]
public sealed partial class EnterImperialVentCrawlerDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ExitImperialVentCrawlerDoAfterEvent : SimpleDoAfterEvent;
