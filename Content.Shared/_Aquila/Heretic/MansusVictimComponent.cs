using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Heretic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MansusVictimComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ArrivalTime;

    [DataField, AutoNetworkedField]
    public TimeSpan? LeaveTime;

    [DataField, AutoNetworkedField]
    public TimeSpan FadeInDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutDuration = TimeSpan.FromSeconds(3);
}
