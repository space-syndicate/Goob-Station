using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Minigames.Events;


public sealed partial class AfterMinigameAddedEvent : EventArgs
{
    public ProtoId<MinigamePrototype> MinigamePrototype = new();

    public EntityUid NewPlayer;
}
