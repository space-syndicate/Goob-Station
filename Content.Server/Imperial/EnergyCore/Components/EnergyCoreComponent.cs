using Robust.Shared.Audio;
using Color = Robust.Shared.Maths.Color;
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
    [ViewVariables(VVAccess.ReadWrite)]
    public float CoreTemp = 0f;

    // Ближайший терминал
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Controller;

    // Ближайший терминал: время данное на поиск
    [DataField]
    public TimeSpan SearchTime = TimeSpan.FromSeconds(5);

    // Минимальная температура, которая может быть у ядра
    [ViewVariables(VVAccess.ReadOnly)]
    public float MinCoreTemp = 0f;

    // Автосистема, определяется через терминал
    [ViewVariables(VVAccess.ReadOnly)]
    public bool AutoSystemActive = false;

    // Изменение температуры (в сек.)
    [ViewVariables(VVAccess.ReadOnly)]
    public float TempChangeMultiplier = 150f;

    // Изменение температуры (в сек.) (ивент расплавления)
    [ViewVariables(VVAccess.ReadOnly)]
    public float TempChangeMultiplierMeltdown = 4000f;

    // Изменение температуры (в сек.) (инициализация защитного протокола)
    [ViewVariables(VVAccess.ReadOnly)]
    public float TempChangeMultiplierProtocol = 4500f;

    // Изменение температуры, нужен для расчета <float TempChangeMultiplier> в положительную или отрицательную сторону
    [ViewVariables(VVAccess.ReadOnly)]
    public float UpdateTemp = 0f;

    // Изменение температуры, будет ли после порога 600000 активирован протокол защиты
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSafeProtocolActive = true;

    // Было ли анансировано, что протокол был отключен
    [ViewVariables(VVAccess.ReadOnly)]
    public bool AnnouncedProtocol = false;

    // Объявление при критически высокой температуре
    [ViewVariables(VVAccess.ReadOnly)]
    public bool AnnounceReady = true;

    // Определение enum для света
    [ViewVariables(VVAccess.ReadOnly)]
    public CoreStatusColorVisual CoreColorEnum = CoreStatusColorVisual.OFFLINE;

    // Техническое определение статуса энерго ядра
    [ViewVariables(VVAccess.ReadOnly)]
    public CoreStatus Status = CoreStatus.OFFLINE;

    // Статус изменения температуры. Нагревание, охлаждание, авто-режим
    [ViewVariables(VVAccess.ReadOnly)]
    public CoreTempChangeLevel TempRiseStatus = CoreTempChangeLevel.COOLING;

    // Свет источяемый ядром
    [DataField]
    public Color CoreColor = Color.FromHex("#74aeff");

    // Сила света
    [DataField]
    public float CoreColorEnergy = 10f;

    // Радиус света
    [DataField]
    public float CoreColorRadius = 10f;

    // Предупреждение о критическом перегреве
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier CriticalHighAnnounce = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/core-criticalhigh.ogg");

    // Базовый эмбиент ядра
    [DataField]
    public SoundSpecifier CoreAmbience1 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_1.ogg");

    // Датафилды цветов

    [DataField]
    public Color OfflineColor = Color.FromHex("#74aeff");

    [DataField]
    public Color IdleColor = Color.FromHex("#74aeff");

    [DataField]
    public Color StableColor = Color.FromHex("#d80000ff");

    [DataField]
    public Color OptimalColor = Color.FromHex("#ff0000ff");

    [DataField]
    public Color ModerateColor = Color.FromHex("#fff700ff");

    [DataField]
    public Color HighColor = Color.FromHex("#fbff11ff");

    [DataField]
    public Color CriticalHighColor = Color.FromHex("#fdff8fff");

    [DataField]
    public Color CompromisedColor = Color.FromHex("#ffffffff");

    [DataField]
    public Color ProtocolColor = Color.FromHex("#fff700ff");
}
