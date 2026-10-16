namespace Content.Goobstation.Shared._Aquila.Xenobiology;

[ByRefEvent]
public readonly record struct SlimeScannedEvent(EntityUid User, EntityUid Scanner);
