using Robust.Shared.Player;
using Robust.Shared.Network;

namespace Content.Server.Imperial.XxRaay.SyndieBattle;

[RegisterComponent]
public sealed partial class SyndieBattleScoreComponent : Component
{
    [DataField]
    public int Score;

    [DataField]
    public int KillCount;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public double SpawnTime;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SurvivalTime;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Alive = true;
}
