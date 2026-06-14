using Content.Shared.Examine;
using Content.Shared.Imperial.EmergencyButton.Components;

namespace Content.Shared.Imperial.EmergencyButton;

public sealed class EmergencyButtonSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmergencyButtonComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<EmergencyButtonComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(entity.Comp.CurrentCharges != 0
            ? Loc.GetString("alert-emergency-button-yes-charges", ("charges", entity.Comp.CurrentCharges))
            : Loc.GetString("alert-emergency-button-no-charges"));
    }
}
