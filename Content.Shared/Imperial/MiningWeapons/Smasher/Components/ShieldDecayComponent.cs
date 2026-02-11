namespace Content.Shared.Imperial.MiningWeapons.Smasher.Components;

/// <summary>
/// Added to the user when the Smasher's Decay state is triggered.
/// This is used to track the removal of the visual effect when the Smasher is not in hand.
/// </summary>
[RegisterComponent]
public sealed partial class ShieldDecayComponent : Component
{
    [ViewVariables]
    public TimeSpan DecayEndTime;
}
