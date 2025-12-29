using Robust.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore;
/// <summary>
/// Система энергетического ядра.
/// </summary>
namespace Content.Server.Imperial.EnergyCore.Components;

[RegisterComponent]
[Access(typeof(EnergyCoreSystem))]
public sealed partial class EnergyCoreComponent : Component
{
    /// <summary>
    /// Температура ядра
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CoreTemp = 0f;

    // Ближайший терминал
    [DataField]
    public EntityUid? Controller;

    // Ближайший терминал: время данное на поиск
    [DataField]
    public TimeSpan SearchTime = TimeSpan.FromSeconds(5);

    // Минимальная температура, которая может быть у ядра
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float MinCoreTemp = 0f;

    // Автосистема, определяется через терминал
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool AutoSystemActive = false;

    // Изменение температуры (в сек.)
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float TempChangeMultiplier = 150f;

    // Изменение температуры (в сек.) (ивент расплавления)
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float TempChangeMultiplierMeltdown = 4000f;

    // Изменение температуры (в сек.) (инициализация защитного протокола)
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float TempChangeMultiplierProtocol = 4500f;

    // Изменение температуры, нужен для расчета <float TempChangeMultiplier> в положительную или отрицательную сторону
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float UpdateTemp = 0f;

    // Изменение температуры, будет ли после порога 600000 активирован протокол защиты
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsSafeProtocolActive = true;

    // Было ли анансировано, что протокол был отключен
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool AnnouncedProtocol = false;

    // Объявление при критически высокой температуре
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool AnnounceReady = true;

    // Определение enum для света
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CoreStatusColorVisual CoreColorEnum = CoreStatusColorVisual.OFFLINE;

    // Техническое определение статуса энерго ядра
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CoreStatus Status = CoreStatus.OFFLINE;

    // Статус изменения температуры. Нагревание, охлаждание, авто-режим
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CoreTempChangeLevel TempRiseStatus = CoreTempChangeLevel.COOLING;

    // Значение передаваемое от ближайшего терминала
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public byte TempChangeStatus = 1;

    // Свет источяемый ядром
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Color CoreColor = Color.FromHex("#74aeff");

    // Сила света
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float CoreColorEnergy = 10f;

    // Радиус света
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float CoreColorRadius = 10f;

    // Предупреждение о критическом перегреве
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier CriticalHighAnnounce = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/core-criticalhigh.ogg");

    // Базовый эмбиент ядра
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier CoreAmbience1 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_1.ogg");

    // Датафилды цветов

    [DataField]
    public string OfflineColor = "#74aeff";

    [DataField]
    public string IdleColor = "#74aeff";

    [DataField]
    public string StableColor = "#d80000ff";

    [DataField]
    public string OptimalColor = "#ff0000ff";

    [DataField]
    public string ModerateColor = "#fff700ff";

    [DataField]
    public string HighColor = "#fbff11ff";

    [DataField]
    public string CriticalHighColor = "#fdff8fff";

    [DataField]
    public string CompromisedColor = "#ffffffff";

    [DataField]
    public string ProtocolColor = "#fff700ff";
}
