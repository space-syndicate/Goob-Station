namespace Content.Goobstation.Common.Cuffs;

[ByRefEvent]
public record struct ModifyCuffDurationEvent(EntityUid Target, float Duration);

// CorvaxGoob : Впадлу делать отдельный файл
[ByRefEvent]
public record struct ModifyBeingCuffDurationEvent(EntityUid User, float Duration);
