using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

[RegisterComponent, Access(typeof(AtmSystem))]
public sealed partial class AtmComponent : Component
{
    /// <summary>
    /// Стопка наличных, которую банкомат выдаёт и принимает.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> CashStack = "Credit";

    /// <summary>
    /// Звук при снятии кредитов.
    /// </summary>
    [DataField]
    public SoundSpecifier? WithdrawSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/coindrop.ogg", AudioParams.Default.WithVolume(-3f));

    /// <summary>
    /// Звук при внесении кредитов.
    /// </summary>
    [DataField]
    public SoundSpecifier? DepositSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/cashregister.ogg", AudioParams.Default.WithVolume(-2f));

    /// <summary>
    /// Звук отказа в операции.
    /// </summary>
    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_prompt_deny.ogg", AudioParams.Default.WithVolume(-7f));

    /// <summary>
    /// Описание снятия кредитов в истории операций.
    /// </summary>
    [DataField]
    public LocId WithdrawReason = "bank-transaction-atm-withdraw";

    /// <summary>
    /// Описание внесения кредитов в истории операций.
    /// </summary>
    [DataField]
    public LocId DepositReason = "bank-transaction-atm-deposit";
}
