using Content.Shared.Atmos;

namespace Content.Server.Imperial.Power.Components;

/// <summary>
/// Конфигурация газовых эффектов суперматерии.
/// </summary>
[RegisterComponent]
public sealed partial class SupermatterGasComponent : Component
{
    /// <summary>
    /// Порог молей газа в камере, при котором эффект считается активным.
    /// </summary>
    [DataField]
    public float GasActivationMoles = 15f;

    /// <summary>
    /// Сколько молей активного газа тратится в секунду.
    /// </summary>
    [DataField]
    public float GasConsumptionPerSecond = 3f;

    /// <summary>
    /// Множитель самовосстановления целостности при наличии термониума.
    /// Количество целостности в секунду при 100% эффекте.
    /// </summary>
    [DataField]
    public float ThermoniumIntegrityRegenPerSecond = 0.5f;

    /// <summary>
    /// Множитель радиации при наличии озона.
    /// </summary>
    [DataField]
    public float OzoneRadiationMultiplier = 1.5f;

    /// <summary>
    /// Множитель радиации при наличии плазмы.
    /// По умолчанию -50% радиации.
    /// </summary>
    [DataField]
    public float PlasmaRadiationMultiplier = 0.5f;

    /// <summary>
    /// Активен ли полный режим отключения суперматерии анти-ноблием.
    /// </summary>
    [DataField]
    public bool AntiNobliumHardShutdownEnabled = true;

    /// <summary>
    /// Множитель количества молний при наличии трития в камере суперматерии.
    /// </summary>
    [DataField]
    public float TritiumLightningMultiplier = 2f;

    /// <summary>
    /// Множитель скорости наступления случайных событий при наличии водяного газа.
    /// Значение 2 означает, что события происходят в 2 раза чаще.
    /// </summary>
    [DataField]
    public float WaterVaporEventSpeedMultiplier = 2f;

    /// <summary>
    /// Таймер для тиков расхода газов.
    /// </summary>
    public TimeSpan GasTickAccumulator = TimeSpan.Zero;

    /// <summary>
    /// Была ли суперматерия выключена антиноблием.
    /// </summary>
    public bool WasShutdownByAntiNoblium = false;

    /// <summary>
    /// Кэши газовых смесей из последнего обновления атмосферы.
    /// </summary>
    public GasMixture? CachedGasMixture;
}


