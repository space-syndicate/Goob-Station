using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.DataDefinitions;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BankTransaction
{
    /// <summary>
    /// Время операции от начала раунда.
    /// </summary>
    [DataField]
    public TimeSpan Time;

    /// <summary>
    /// Сумма операции: положительная при зачислении, отрицательная при списании.
    /// </summary>
    [DataField]
    public int Amount;

    /// <summary>
    /// Описание операции.
    /// </summary>
    [DataField]
    public LocId Reason;

    /// <summary>
    /// Имя второй стороны операции, если она есть.
    /// </summary>
    [DataField]
    public string? Party;

    /// <summary>
    /// Пояснение к операции, например причина премии или штрафа.
    /// </summary>
    [DataField]
    public string? Comment;
}
