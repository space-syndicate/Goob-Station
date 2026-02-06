using Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks;

namespace Content.Server.Imperial.MiningWeapons.EmpoweredAttacks;

public sealed partial class AttacksSystem : SharedAttacksSystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializePiercingLunge();
    }
}
