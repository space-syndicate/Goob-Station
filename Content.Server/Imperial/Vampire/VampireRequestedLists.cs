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
        { 4, 500 },
        { 5, 500 }
    };

    // способности Hemomancer
    public static readonly List<EntProtoId> Hemomancer = new()
    {
        "VampireTentaclesAction",
        "VampireTransformBatAction",
        "VampireTransformBloodAction",
        "VampireNosferatyAction"
        // "обратиться",
    };

    // способности Umbrae
    public static readonly List<EntProtoId> Umbrae = new()
    {
        "VampireUnCuffAction",
        "VampireInvisibleAction",
        "VampireShadowTrapAction",
        "VampireCloneAction"
    };

    // способности Gargantua
    public static readonly List<EntProtoId> Gargantua = new()
    {
        "RushBloodAction",
        "VampireTeleportAction",
        "VampireUnCuffAction",
        "VampireReconciliationAction",
        "VampireNosferatyAction"
        // "обратиться",
    };
}
