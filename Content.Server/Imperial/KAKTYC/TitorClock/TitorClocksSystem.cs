using Content.Shared.Examine;

namespace Content.Server.Imperial.KAKTYC.TitorClock;

public sealed class TitorClockSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TitorClockComponent, ExaminedEvent>(GeneratorExamined);
    }
    private void GeneratorExamined(EntityUid uid, TitorClockComponent component, ExaminedEvent args)
    {
        var mapID = (float)_transform.GetMapId(uid);
        component.UniversalNumber = mapID * component.CoefficentNumberTwo;
        var number = component.UniversalNumber / mapID * component.CoefficentNumber;
        component.TitorNumber = number;
        args.PushMarkup(Loc.GetString($"titor-clock-line", ("number", component.TitorNumber)));
    }
}
