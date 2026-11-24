using Content.Shared.Item;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._CorvaxGoob.EvidenceBag;

[RegisterComponent]
public sealed partial class EvidenceBagComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<ItemSizePrototype>, Vector2> SizesInfo = new()
        {
            { "Tiny", new Vector2(1, 1) },
            { "Small", new Vector2(1, 1) },
            { "Normal", new Vector2(0.75f, 0.75f) },
            { "Huge", new Vector2(0.5f, 0.5f) },
            { "Ginormous", new Vector2(0.25f, 0.25f) },
        };

    [DataField]
    public string Slot = "slot";
}
