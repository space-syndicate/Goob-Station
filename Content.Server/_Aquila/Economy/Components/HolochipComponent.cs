using Content.Server._Aquila.Economy.Systems;
using Robust.Shared.Audio;

namespace Content.Server._Aquila.Economy.Components;

/// <summary>
/// Голочип с кредитами, которые можно вернуть на банковский счёт, приложив чип к ID-карте или КПК.
/// </summary>
[RegisterComponent, Access(typeof(HolochipSystem))]
public sealed partial class HolochipComponent : Component
{
    /// <summary>
    /// Описание внесения голочипа в истории операций.
    /// </summary>
    [DataField]
    public LocId DepositReason = "bank-transaction-chip-deposit";

    /// <summary>
    /// Звук внесения голочипа на счёт.
    /// </summary>
    [DataField]
    public SoundSpecifier? DepositSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_success.ogg", AudioParams.Default.WithVolume(-7f));
}
