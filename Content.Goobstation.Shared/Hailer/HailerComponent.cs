// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Hailer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HailerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId HailerAction = "ActionHailer";

    [DataField, AutoNetworkedField]
    public EntityUid? HailActionEntity;

    // CorvaxGoob-HailerRework-Start
    [DataField]
    public string? SelectedMode = null;

    [DataField]
    public Dictionary<string, ProtoId<LocalizedDatasetPrototype>> AvailableModes = new();

    [DataField]
    public Dictionary<string, ProtoId<LocalizedDatasetPrototype>>? EmagedModes;

    [DataField]
    public string? EmaggedTTS = "Omnotron";
    // CorvaxGoob-HailerRework-End
}


