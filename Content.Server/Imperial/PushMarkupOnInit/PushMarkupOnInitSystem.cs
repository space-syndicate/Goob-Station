using Content.Shared.Examine;
using Content.Server.Imperial.PushMarkupOnInit;

public sealed class PushMarkupOnInitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PushMarkupOnInitComponent, ExaminedEvent>(OnExamined);
    }
    private void OnExamined(EntityUid uid, PushMarkupOnInitComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(comp.Markup));
    }
}