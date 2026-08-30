using Content.Client.IconSmoothing;
using Content.Client.Storage.Visualizers;
using Content.Shared.SprayPainter.Prototypes;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client._CorvaxGoob.IconSmoothing;

public sealed class IconSmoothVisualizerSystem : VisualizerSystem<IconSmoothComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;

    protected override void OnAppearanceChange(EntityUid uid,
        IconSmoothComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var prototype, args.Component))
        {
            if (_prototypeManager.Resolve(prototype, out var proto))
            {
                if (proto.TryGetComponent(out SpriteComponent? sprite, _componentFactory) && proto.TryGetComponent(out IconSmoothComponent? smooth, _componentFactory))
                {
                    comp.StateBase = smooth.StateBase;

                    var tempUid = Spawn(prototype);
                    SpriteSystem.CopySprite(tempUid, uid);
                    QueueDel(tempUid);

                    _iconSmooth.DirtyNeighbours(uid);
                }
            }
        }
    }
}
