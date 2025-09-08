using Content.Shared.Imperial.EmergencyButton.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Imperial.EmergencyButton;

public sealed class EmergencyButtonSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmergencyButtonComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EmergencyButtonComponent, ComponentShutdown>(OnComponentShutdown);

    }

    private void OnComponentStartup(EntityUid uid, EmergencyButtonComponent component, ComponentStartup args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, EmergencyButtonComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            _sprite.LayerSetRsiState((uid, sprite), 0, "EmergencyButton");
        }
    }

    private void UpdateAppearance(EntityUid uid, EmergencyButtonComponent component)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.LayerSetRsiState((uid, sprite), 0, "EmergencyButton");
    }
}

