using System;
using System.Collections.Generic;

namespace Content.Shared.Imperial.XxRaay.Nda079;

/// <summary>
/// Configuration for NDA079 ability values at different CPU levels.
/// </summary>
public static class NDA079LevelConfig
{
    public sealed class LightFlickerConfig
    {
        public TimeSpan LightOffDuration { get; init; }
        public float Radius { get; init; }
        public float SuccessChance { get; init; }
        public TimeSpan Cooldown { get; init; }
    }

    public sealed class AirlockConfig
    {
        public TimeSpan BoltDuration { get; init; }
        public float SuccessChance { get; init; }
    }

    private static readonly Dictionary<int, LightFlickerConfig> LightFlickerConfigs = new()
    {
        { 2, new LightFlickerConfig { LightOffDuration = TimeSpan.FromSeconds(15), Radius = 20f, SuccessChance = 0.95f, Cooldown = TimeSpan.FromSeconds(40) } },
        { 3, new LightFlickerConfig { LightOffDuration = TimeSpan.FromSeconds(30), Radius = 35f, SuccessChance = 1.0f, Cooldown = TimeSpan.FromSeconds(70) } },
        { 4, new LightFlickerConfig { LightOffDuration = TimeSpan.FromSeconds(60), Radius = 45f, SuccessChance = 1.0f, Cooldown = TimeSpan.FromSeconds(90) } },
    };

    private static readonly Dictionary<int, AirlockConfig> AirlockConfigs = new()
    {
        { 2, new AirlockConfig { BoltDuration = TimeSpan.FromSeconds(10), SuccessChance = 0.95f } },
        { 3, new AirlockConfig { BoltDuration = TimeSpan.FromSeconds(30), SuccessChance = 1.0f } },
        { 4, new AirlockConfig { BoltDuration = TimeSpan.FromSeconds(60), SuccessChance = 1.0f } },
    };

    public static LightFlickerConfig? GetLightFlickerConfig(int level)
    {
        return LightFlickerConfigs.GetValueOrDefault(level);
    }

    public static AirlockConfig? GetAirlockConfig(int level)
    {
        return AirlockConfigs.GetValueOrDefault(level);
    }
}

