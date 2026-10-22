// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Add order to database.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleAddOrderMessage : BoundUserInterfaceMessage
{
    // CorvaxGoob-CargoFeatures-Start
    public string? Requester;
    public string? DeliveryDestination;
    public string? Note;
    public bool SecuredDelivery;
    // CorvaxGoob-CargoFeatures-End

    public string CargoProductId;
    public int Amount;

    // CorvaxGoob-CargoFeatures 
    public CargoConsoleAddOrderMessage(string? requester, string? deliveryDestination, string? note, string cargoProductId, int amount, bool securedDelivery = false)
    {
        // CorvaxGoob-CargoFeatures-Start
        Requester = requester;
        DeliveryDestination = deliveryDestination;
        Note = note;
        SecuredDelivery = securedDelivery;
        // CorvaxGoob-CargoFeatures-End

        CargoProductId = cargoProductId;
        Amount = amount;
    }
}
