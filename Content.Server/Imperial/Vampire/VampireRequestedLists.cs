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
        "VampireMessageForGhouls",
        "VampireBloodTheftAction",
        "VampireRecoveryAction",
        "VampireSleep"
    };

    // способности Hemomancer
    public static readonly List<EntProtoId> Hemomancer = new()
    {
        "VampireTentacles",
        "VampireTransformBat",
        "VampireTransformBlood",
        "VampireNosferaty"
    };

    // способности Umbrae
    public static readonly List<EntProtoId> Umbrae = new()
    {
        "VampireUnCuff",
        "VampireInvisible",
        "VampireShadowTrapAction",
        "VampireClone"
    };

    // способности Gargantua
    public static readonly List<EntProtoId> Gargantua = new()
    {
        "RushBlood",
        "VampireTeleport",
        "VampireUnCuff",
        "VampireReconciliationAction",
        "VampireNosferaty"
    };
}
