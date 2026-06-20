using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Imperial.EmergencyButton;
using Content.Shared.Imperial.EmergencyButton.Components;
using Content.Shared.Imperial.EmergencyButton.Events;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.EmergencyButton;

public sealed class EmergencyButtonServerSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;
    [Dependency] private readonly NavMapSystem _navMap = null!;
    [Dependency] private readonly AudioSystem _audioSystem = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyButtonComponent, EmergencyButtonPressedEvent>(OnEmergencyButtonPressed);
    }

    private void OnEmergencyButtonPressed(Entity<EmergencyButtonComponent> entity, ref EmergencyButtonPressedEvent args)
    {
        var officerName = Identity.Name(args.User, EntityManager);

        var location = _navMap.TryGetNearestBeacon((args.User, null), out var beacon, out _)
            ? beacon.Value.Comp.Text!
            : Loc.GetString("alert-emergency-button-popup-unknown-location");

        var message = Loc.GetString("alert-emergency-button-popup-message",
            ("officerName", officerName),
            ("location", location));

        _audioSystem.PlayPvs(entity.Comp.UseSound, Transform(entity).Coordinates);

        if (_prototype.TryIndex(entity.Comp.RadioChannel, out var radioChannel))
            _radio.SendRadioMessage(entity, message, radioChannel, entity);
    }
}
