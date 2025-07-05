using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Imperial.PiratesNewHorizon.StatusIcons;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Imperial.PiratesNewHorizon.Pirate;
namespace Content.Client.Imperial.PiratesNewHorizon.StatusIcons;

public sealed class PirateStatusIconsSystem : SharedPirateSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateComponent, GetStatusIconsEvent>(GetPirateIcon);
        SubscribeLocalEvent<PirateCaptainComponent, GetStatusIconsEvent>(GetPirateCaptainIcon);
    }

    private void GetPirateIcon(Entity<PirateComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void GetPirateCaptainIcon(Entity<PirateCaptainComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<PirateComponent>(ent))
            return;

        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}