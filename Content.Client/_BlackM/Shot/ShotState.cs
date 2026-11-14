using System.Numerics;
using Content.Shared._BlackM.Shot;
using Robust.Shared.Map;

namespace Content.Client._BlackM.Shot;

public sealed class ShotState
{
    public ShotComponent Comp = default!;
    public MapId Map;
    public Vector2 Pos;
    public Vector2 Dir;
    public float Age;
    public float Dist;
    public float TrailAcc;
    public bool Visible = true;
    public float Phase;

    public bool Smoke = true;
}
