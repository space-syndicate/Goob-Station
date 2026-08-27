namespace Content.Goobstation.Common.Footprints;

[RegisterComponent]
public sealed partial class CorvaxFootprintOwnerComponent : Component
{
    /// <summary>
    ///     Максимальное количество реагента на ногах
    /// </summary>
    [DataField]
    public float MaxFootVolume = 10;

    /// <summary>
    ///     Максимальное количество реагента на теле
    /// </summary>
    [DataField]
    public float MaxBodyVolume = 20;

    /// <summary>
    ///     Минимум реагента для отрисовки следа ноги
    /// </summary>
    [DataField]
    public float MinFootprintVolume = 0.5f;

    /// <summary>
    ///     Максимум реагента для следа ноги.
    ///     так же учавствует в нормолизации альфа канала для насыщенности цвета следа
    /// </summary>
    [DataField]
    public float MaxFootprintVolume = 1;

    /// <summary>
    ///     Максимум реагента для следа тела
    /// </summary>
    [DataField]
    public float MinBodyprintVolume = 2;

    /// <summary>
    ///     Максимум реагента для следа тела.
    ///     так же учавствует в нормолизации альфа канала для насыщенности цвета следа
    /// </summary>
    [DataField]
    public float MaxBodyprintVolume = 5;

    /// <summary>
    ///     Сколько тайлов надо приодолеть стоя, чтобы оставить следующий отпечаток. 
    ///     Чем меньше, тем следы чаще
    /// </summary>
    [DataField]
    public float FootDistance = 0.5f;

    /// <summary>
    ///     Сколько тайлов надо приодолеть ползком, чтобы оставить следующий отпечаток. 
    ///     Чем меньше, тем следы чаще
    /// </summary>
    [DataField]
    public float BodyDistance = 1;

    /// <summary>
    ///     Счётчик пройденного пути
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Distance;

    /// <summary>
    ///     Боковое смещение следующей стопы, нужно для чередования ног
    /// </summary>
    [DataField]
    public float NextFootOffset = 0.0625f;

    /// <summary>
    ///     Прототип декали для следов, оставляемых стоя
    /// </summary>
    [DataField]
    public string FootDecalId = "FootprintForward";

    /// <summary>
    ///     Прототип декали для следов тела, при размазывании телом
    /// </summary>
    [DataField]
    public string BodyDecalId = "FootprintBody";

    /// <summary>
    ///     Количество разрешённых направлений следа. Угол движения прижимается к ближайшему из них,
    ///     что позволяет перекрывающимся следам сливаться. 4 с офсетом по умолчанию даёт 45/135/225/315.
    /// </summary>
    [DataField]
    public int DirectionCount = 8;

    /// <summary>
    /// Угол (в градусах) первого разрешённого направления, или же шаг. Если null, используется половина шага
    /// (например 45 для 4 направлений, 22.5 для 8).
    /// </summary>
    [DataField]
    public float? DirectionOffsetDegrees;

    /// <summary>
    ///     Постоянный угол (в градусах), добавляемый перед квантованием, для корректировки декали
    /// </summary>
    [DataField]
    public float SpriteAngleOffset;

    /// <summary>
    ///     Радиус (в тайлах), внутри которого следы одного направления сливаются вместо спавна новой декали.
    /// </summary>
    [DataField]
    public float GroupRadius = 0.35f;

    /// <summary>Насколько сильно цвет нового следа подмешивается в уже слитый (0..1).</summary>
    [DataField]
    public float DecalBlendFactor = 0.4f;

    /// <summary>Сколько альфы добавляет каждый слитый след, делая насыщенные места плотнее.</summary>
    [DataField]
    public float DecalAlphaAccumulation = 0.2f;
}
