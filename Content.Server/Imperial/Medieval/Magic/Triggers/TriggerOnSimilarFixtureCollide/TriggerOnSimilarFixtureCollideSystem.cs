using Content.Server.Explosion.EntitySystems;
using Content.Shared.Trigger;
using Robust.Shared.Physics.Events;

namespace Content.Server.Imperial.Medieval.Magic.Triggers;


public sealed partial class TriggerOnSimilarFixtureCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSimilarFixtureCollideComponent, StartCollideEvent>(OnTriggerCollide);
    }

    private void OnTriggerCollide(EntityUid uid, TriggerOnSimilarFixtureCollideComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != component.FixtureID) return;
        if ((component.OtherFixtureID ?? args.OurFixtureId) != args.OtherFixtureId) return;

        var triggerEvent = new TriggerEvent(args.OtherEntity);
        RaiseLocalEvent(uid, ref triggerEvent, true);
    }
}
