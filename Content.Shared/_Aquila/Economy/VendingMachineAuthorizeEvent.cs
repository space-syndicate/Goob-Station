namespace Content.Shared._Aquila.Economy;

[ByRefEvent]
public record struct VendingMachineAuthorizeEvent(EntityUid User, bool Authorized = false);
