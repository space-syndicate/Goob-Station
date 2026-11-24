using Content.Client.Alerts;
using Content.Shared._CorvaxGoob.Hostage;
using Content.Shared._CorvaxGoob.Surrender;
using Content.Shared.Alert;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.Hostage;

public sealed partial class HostageSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HostageTakerComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<HostageTakerComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Comp.Weapon, out var gunSprite))
            return;

        if (ent.Comp.ExecuteAlert == args.Alert.ID)
            if (_proto.TryIndex<AlertPrototype>(ent.Comp.ExecuteAlert, out var alert))
                _sprite.CopySprite((ent.Comp.Weapon, gunSprite), (args.SpriteViewEnt.Owner, args.SpriteViewEnt.Comp));
    }
}
