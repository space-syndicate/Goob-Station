using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.DoAfter;
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
    // Температура энергетического ядра
    [DataField]
    public float CoreTemp = 0f;

    // Минимальная температура, которая может быть у ядра
    [DataField]
    public float MinCoreTemp = 0f;

    // Автосистема, определяется через терминал
    [DataField]
    public bool AutoSystemActive = false;

    // Изменение температуры (в сек.)
    [DataField]
    public float TempChangeMultiplier = 150f;

    // Изменение температуры (в сек.) (ивент расплавления)
    [DataField]
    public float TempChangeMultiplierMeltdown = 4000f;

    // Изменение температуры (в сек.) (инициализация защитного протокола)
    [DataField]
    public float TempChangeMultiplierProtocol = 4500f;

    // Изменение температуры, нужен для расчета <float TempChangeMultiplier> в положительную или отрицательную сторону
    [DataField]
    public float UpdateTemp = 0f;

    // Изменение температуры, будет ли после порога 600000 активирован протокол защиты
    [DataField]
    public bool IsSafeProtocolActive = true;

    // Было ли анансировано, что протокол был отключен
    [DataField]
    public bool AnnouncedProtocol = false;

    // Определение enum для света
    [DataField]
    public CoreStatusColorVisual CoreColorEnum = CoreStatusColorVisual.OFFLINE;

    // Техническое определение статуса энерго ядра
    [DataField]
    public CoreStatus Status = CoreStatus.OFFLINE;

    // Статус изменения температуры. Нагревание, охлаждание, авто-режим
    [DataField]
    public CoreTempChangeLevel TempRiseStatus = CoreTempChangeLevel.COOLING;

    // Значение передаваемое от ближайшего терминала
    [DataField]
    public byte TempChangeStatus = 1;

    // Свет источяемый ядром
    [DataField]
    public Color CoreColor = Color.FromHex("#74aeff");

    // Сила света
    [DataField]
    public float CoreColorEnergy = 10f;

    // Радиус света
    [DataField]
    public float CoreColorRadius = 10f;

    // Базовый эмбиент ядра
    [DataField]
    public SoundSpecifier CoreAmbience1 = new SoundPathSpecifier("/Audio/Imperial/EnergyCore/CoreAmbience/coreambience_1.ogg");
}
