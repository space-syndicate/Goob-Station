
using Content.Shared.Examine;
using Content.Server.Cargo;
using Content.Server.Imperial.PiratesNewHorizon.GPS.Components;
namespace Content.Server.Imperial.PiratesNewHorizon.GPS.Systems;

public sealed class GPSTrackerPriceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GPSTrackerPriceComponent, ExaminedEvent>(OnExaminedEvent);
    }
    private void OnExaminedEvent(EntityUid uid, GPSTrackerPriceComponent component, ExaminedEvent args)
    {
        if (component.GPSTrackerInstalled == false)
            args.PushMarkup(Loc.GetString("gpstracker-examine-missing"));
        else
            args.PushMarkup(Loc.GetString("gpstracker-examine-installed"));
    }
}
