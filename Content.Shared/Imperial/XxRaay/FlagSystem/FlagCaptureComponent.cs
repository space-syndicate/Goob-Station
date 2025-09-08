using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.FlagSystem;

/// <summary>
/// Компонент для механики захвата флага
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedFlagCaptureSystem))]
public sealed partial class FlagCaptureComponent : Component
{
    /// <summary>
    /// Радиус захвата флага в тайлах
    /// </summary>
    [DataField]
    public float CaptureRadius = 2.0f;

    /// <summary>
    /// Время захвата в секундах
    /// </summary>
    [DataField]
    public TimeSpan CaptureTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Текущий прогресс захвата
    /// </summary>
    [DataField]
    public TimeSpan CaptureProgress = TimeSpan.Zero;

    /// <summary>
    /// Идет ли процесс захвата
    /// </summary>
    [DataField]
    public bool IsBeingCaptured = false;

    /// <summary>
    /// Можно ли захватить этот флаг
    /// </summary>
    [DataField]
    public bool CanBeCaptured = true;

    /// <summary>
    /// Последнее время проверки игроков рядом
    /// </summary>
    public TimeSpan LastCheckTime = TimeSpan.Zero;

    /// <summary>
    /// Интервал опроса игроков вокруг флага (секунды)
    /// </summary>
    [DataField]
    public float ScanIntervalSeconds = 0.5f;

    /// <summary>
    /// Последнее время сканирования окружения
    /// </summary>
    public TimeSpan LastScanTime = TimeSpan.Zero;
}

/// <summary>
/// Состояние компонента для синхронизации между клиентом и сервером
/// </summary>
[Serializable, NetSerializable]
public sealed class FlagCaptureComponentState : ComponentState
{
    public TimeSpan CaptureProgress;
    public bool IsBeingCaptured;

    public FlagCaptureComponentState(TimeSpan captureProgress, bool isBeingCaptured)
    {
        CaptureProgress = captureProgress;
        IsBeingCaptured = isBeingCaptured;
    }
}
