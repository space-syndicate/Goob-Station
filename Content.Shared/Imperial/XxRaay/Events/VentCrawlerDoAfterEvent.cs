using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Events;

[Serializable, NetSerializable]
public sealed partial class EnterVentCrawlerDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ExitVentCrawlerDoAfterEvent : SimpleDoAfterEvent;
