using System.ComponentModel;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Minigames.Events;


public sealed partial class BeforeMinigameAddedEvent : CancelEventArgs
{
    public ProtoId<MinigamePrototype> MinigamePrototype = new();

    public EntityUid NewPlayer;
}
