using Content.Server.GameTicking.Rules.Components;
using System.Collections.Generic;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Imperial.XxRaay.SyndieBattle;

/// <summary>
/// Компонент для правила игры SyndieBattle
/// </summary>
[RegisterComponent]
public sealed partial class SyndieBattleRuleComponent : Component
{
    /// <summary>
    /// Выдавать ли аплинк предателям
    /// </summary>
    [DataField]
    public bool GiveUplink = true;

    /// <summary>
    /// Выдавать ли кодовые слова предателям
    /// </summary>
    [DataField]
    public bool GiveCodewords = true;

    /// <summary>
    /// Выдавать ли брифинг предателям
    /// </summary>
    [DataField]
    public bool GiveBriefing = true;

    /// <summary>
    /// Активно ли правило в данный момент
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Active;

    /// <summary>
    /// Количество машин искупления
    /// </summary>
    [DataField]
    public int RedemptionMachineCount = 30;

    /// <summary>
    /// Длительность пацифизма в секундах (в начале раунда)
    /// </summary>
    [DataField]
    public int PacifyDurationSeconds = 120;

    /// <summary>
    /// Время старта правила в секундах
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// Что выдается в аплинк
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<CurrencyPrototype> Currency = "Telecrystal";

    /// <summary>
    /// Количество валюты в аплинке раундстартом
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int StartingTelecrystalCount = 35;

    /// <summary>
    /// Стартовая экипировка
    /// </summary>
    [DataField("gear", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string StartingGear = "PassengerGear";

    /// <summary>
    /// Категории аплинка
    /// </summary>
    [DataField]
    public List<string> Categories = new()
    {
        "SyndiebattleUplinkWeaponry",
        "SyndiebattleUplinkAmmo",
        "SyndiebattleUplinkExplosives",
        "SyndiebattleUplinkWearables",
        "SyndiebattleUplinkChemicals",
        "SyndiebattleUplinkDeception",
        "SyndiebattleUplinkDisruption",
        "SyndiebattleUplinkImplants",
        "SyndiebattleUplinkAllies",
        "SyndiebattleUplinkJob",
        "SyndiebattleUplinkPointless",
        "SyndiebattleUplinkCustom",
    };

    [DataField]
    public string RespawnMap = "Maps/Imperial/SyndieBattle/RespawnMap.yml";
}


