using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Vampire;

public static class VampireAbilityLists
{
    public static readonly Dictionary<int, float> AbilityThresholds = new()
    {
        { 0, 100f }, // для получения первой способности нужно выпить 100 ед, для второй 200 и тд
        { 1, 200f },
        { 2, 300f },
        { 3, 400f },
        { 4, 500f }
    };

    // базовые способности
    public static readonly List<EntProtoId> BaseAbilities = new()
    {
        "VampireClawAction",
        // "обратиться",
        "VampireMessageForGhoulsAction",
        "VampireBloodTheftAction",
        "VampireRecoveryAction",
        "VampireSleepAction"
    };

    // способности Hemomancer
    public static readonly List<EntProtoId> Hemomancer = new()
    {
        "VampireTentaclesAction",
        "VampireTransformBatAction",
        "VampireTransformBloodAction",
        "VampireNosferatyAction"
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
    };
}
