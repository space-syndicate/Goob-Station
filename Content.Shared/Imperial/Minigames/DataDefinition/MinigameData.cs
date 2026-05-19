using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Minigames;

[Virtual]
[DataDefinition, Serializable, NetSerializable]
public partial class MinigameData : IEquatable<MinigameData>, ICloneable
{

    [DataField, NonSerialized, ViewVariables(VVAccess.ReadOnly)]
    public ComponentRegistry Minigames = new();

    [DataField, NonSerialized, ViewVariables(VVAccess.ReadOnly)]
    public ComponentRegistry ComponentBlackList = new();


    [DataField]
    public float MinMinigamePrecentToWon = 1.0f;


    [DataField]
    public bool OnlyOneWinner = true;

    [DataField]
    public bool StartInstantly = true;


    [DataField]
    public TimeSpan? MaxMinigamePlaytime;


    [ViewVariables]
    public TimeSpan MinigameStartTime = TimeSpan.FromSeconds(0);


    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MinigameData)
            return false;

        return Equals(obj as MinigameData);
    }

    public bool Equals(MinigameData? other)
    {
        if (other == null) return false;

        return
            StartInstantly == other.StartInstantly &&
            Minigames.Keys.Equals(other.Minigames) &&
            ComponentBlackList.Keys.Equals(other.Minigames) &&
            MaxMinigamePlaytime == other.MaxMinigamePlaytime &&
            OnlyOneWinner == other.OnlyOneWinner;
    }

    public object Clone()
    {
        return new MinigameData()
        {
            Minigames = Minigames,
            ComponentBlackList = ComponentBlackList,
            MaxMinigamePlaytime = MaxMinigamePlaytime,
            MinigameStartTime = MinigameStartTime,
            StartInstantly = StartInstantly,
            OnlyOneWinner = OnlyOneWinner
        };
    }
}
