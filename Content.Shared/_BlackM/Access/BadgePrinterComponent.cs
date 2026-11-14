using System.Collections.Generic;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._BlackM.Access;

[RegisterComponent, NetworkedComponent]
public sealed partial class BadgePrinterComponent : Component
{
    [DataField]
    public string CardSlotId = "badge_printer_card_slot";

    [DataField]
    public string PermitSlotId = "badge_printer_permit_slot";

    [DataField]
    public string PassportSlotId = "badge_printer_passport_slot";

    [DataField]
    public TimeSpan ReprintPassportDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public List<BadgePrinterEntry> AvailableBadges = new();

    [DataField]
    public string OutputSlotId = "badge_printer_output";

    [DataField]
    public int MaxBadgesPerPrint = 6;

    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/vending_restock_done.ogg");

    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public TimeSpan NextPrintTime = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan NextReprintTime = TimeSpan.Zero;

    [ViewVariables]
    public Dictionary<string, int> PrintedCounts = new();
}

[DataDefinition]
public sealed partial class BadgePrinterEntry
{
    [DataField(required: true)]
    public EntProtoId Proto = default!;

    [DataField]
    public string IconRsi = "_BlackM/Objects/Misc/badges.rsi";

    [DataField(required: true)]
    public string IconState = default!;

    [DataField(required: true)]
    public string Department = default!;

    [DataField]
    public string? Warning;

    [DataField]
    public int? Max;
}
