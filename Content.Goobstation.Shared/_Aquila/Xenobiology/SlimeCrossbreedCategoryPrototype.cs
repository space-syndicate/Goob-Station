using Content.Goobstation.Shared.Xenobiology;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology;

[Prototype]
public sealed partial class SlimeCrossbreedCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название категории в сканере слаймов.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Цвет названия категории в сканере слаймов.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Порода слайма, которая превращается в гибриды этой категории.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BreedPrototype> Breed;

    /// <summary>
    /// Сколько экстрактов слайм должен поглотить.
    /// </summary>
    [DataField]
    public int Required = 10;

    /// <summary>
    /// Сколько длится скармливание одного экстракта.
    /// </summary>
    [DataField]
    public TimeSpan FeedDelay = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Звук при образовании гибрида.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    /// <summary>
    /// Скормленный экстракт и получаемый из него гибрид.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, EntProtoId> Results = new();
}
