using Content.Shared.Imperial.Vampire;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Vampire;

public static class VampireAbilityLists
{
    public static readonly Dictionary<int, int> AbilityThresholds = new()
    {
        { 0, 100 }, // для получения первой способности нужно выпить 100 ед, для второй 200 и тд
        { 1, 200 },
        { 2, 300 },
        { 3, 400 },
        { 4, 450 },
        { 5, 500 }
    };

    // способности Hemomancer
    public static readonly List<EntProtoId> Hemomancer = new()
    {
        "VampireTentaclesAction",
        "VampireTransformBatAction",
        "VampireTransformBloodAction",
        "VampireNosferatyAction",
        "VampireTurnAction"
    };

    // способности Umbrae
    public static readonly List<EntProtoId> Umbrae = new()
    {
        "VampireUnCuffAction",
        "VampireInvisibleAction",
        "VampireBloodAnchorAction",
        "VampireShadowTrapAction",
        "VampireCloneAction",
        "VampireTurnAction"
    };

    // способности Gargantua
    public static readonly List<EntProtoId> Gargantua = new()
    {
        "RushBloodAction",
        "VampireTeleportAction",
        "VampireReconciliationAction",
        "VampireNosferatyAction",
        "VampireJerkAction",
        "VampireTurnAction"
    };

    public static readonly EntProtoId VampireSwordPlus = "VampireSwordPlusAction";
    public static readonly EntProtoId VampireInvisiblePlus = "VampireInvisiblePlusAction";
    public static readonly EntProtoId VampireNosferatyPlus = "VampireNosferatyPlusAction";

}
