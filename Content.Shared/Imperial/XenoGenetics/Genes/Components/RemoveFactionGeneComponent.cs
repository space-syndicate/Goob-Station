using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveFactionGeneComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;

    [DataField, AutoNetworkedField]
    public bool HadPacifist = false;

    /// <summary>
    /// Factions this entity is a part of.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
}