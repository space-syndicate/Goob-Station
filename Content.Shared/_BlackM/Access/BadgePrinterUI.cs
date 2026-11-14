using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._BlackM.Access;

[Serializable, NetSerializable]
public enum BadgePrinterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BadgePrinterBuiState : BoundUserInterfaceState
{
    public bool HasCard;
    public bool HasPermit;
    public bool HasPassport;
    public string? PassportOwnerName;
    public List<BadgePrinterOptionData> Options;

    public BadgePrinterBuiState(
        bool hasCard,
        bool hasPermit,
        bool hasPassport,
        string? passportOwnerName,
        List<BadgePrinterOptionData> options)
    {
        HasCard = hasCard;
        HasPermit = hasPermit;
        HasPassport = hasPassport;
        PassportOwnerName = passportOwnerName;
        Options = options;
    }
}

[Serializable, NetSerializable]
public sealed class BadgePrinterOptionData
{
    public string ProtoId;
    public string Name;
    public string Description;
    public string IconRsi;
    public string IconState;
    public string Department;
    public string? Warning;

    public int? Remaining;

    public BadgePrinterOptionData(string protoId, string name, string description, string iconRsi, string iconState, string department, int? remaining = null, string? warning = null)
    {
        ProtoId = protoId;
        Name = name;
        Description = description;
        IconRsi = iconRsi;
        IconState = iconState;
        Department = department;
        Warning = warning;
        Remaining = remaining;
    }
}

[Serializable, NetSerializable]
public sealed class BadgePrinterPrintMessage : BoundUserInterfaceMessage
{
    public List<string> SelectedBadgeProtoIds;

    public BadgePrinterPrintMessage(List<string> selectedBadgeProtoIds)
    {
        SelectedBadgeProtoIds = selectedBadgeProtoIds;
    }
}

[Serializable, NetSerializable]
public sealed class BadgePrinterEjectCardMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class BadgePrinterEjectPassportMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class BadgePrinterReprintPassportMessage : BoundUserInterfaceMessage
{
}
