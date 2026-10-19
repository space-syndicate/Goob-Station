using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Aquila.Economy.Components;

[RegisterComponent, Access(typeof(BankAccountSystem))]
public sealed partial class BankCardComponent : Component
{
    /// <summary>
    /// Прототип счёта, который открывается владельцу карты при выходе на смену.
    /// </summary>
    [DataField]
    public EntProtoId AccountPrototype = "BankAccount";

    /// <summary>
    /// Счёт, привязанный к карте.
    /// </summary>
    [DataField]
    public EntityUid? Account;

    /// <summary>
    /// Стопка, в виде которой с карты снимаются кредиты.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> ChipStack = "Holochip";

    /// <summary>
    /// Описание снятия кредитов с карты в истории операций.
    /// </summary>
    [DataField]
    public LocId ChipWithdrawReason = "bank-transaction-chip-withdraw";

    /// <summary>
    /// Звук снятия кредитов с карты.
    /// </summary>
    [DataField]
    public SoundSpecifier? ChipSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/coindrop.ogg", AudioParams.Default.WithVolume(-3f));

    /// <summary>
    /// Иконка пункта "Снять кредиты" в меню карты.
    /// </summary>
    [DataField]
    public SpriteSpecifier? ChipVerbIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Aquila/Objects/Economy/holochip.rsi"), "holochip");

    /// <summary>
    /// Приоритет пункта "Снять кредиты" среди альтернативных действий карты. Самое приоритетное действие срабатывает по Alt+ЛКМ.
    /// </summary>
    [DataField]
    public int ChipVerbPriority = 10;
}
