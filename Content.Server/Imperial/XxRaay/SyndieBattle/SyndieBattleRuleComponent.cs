using Content.Server.GameTicking.Rules.Components;
using System.Collections.Generic;

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
    /// Время старта правила в секундах (Timing.CurTime.TotalSeconds)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public double StartTime = 0.0;
}


