using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Minigames.Events;


[Serializable, NetSerializable]
public sealed class StartMinigameEvent : EntityEventArgs
{
    public NetEntity Player;

    public ProtoId<MinigamePrototype> MinigamePrototype = default!;
}
