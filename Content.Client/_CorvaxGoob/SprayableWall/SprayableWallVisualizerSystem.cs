using Content.Client.IconSmoothing;
using Content.Shared._CorvaxGoob.SprayableWall;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.SprayableWall;

public sealed class SprayableWallVisualizerSystem : VisualizerSystem<SprayableWallComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;

    protected override void OnAppearanceChange(EntityUid uid,
        SprayableWallComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var prototype, args.Component))
        {
            if (_prototypeManager.Resolve(prototype, out var proto))
            {
                if (TryComp<IconSmoothComponent>(uid, out var origSmooth) && proto.TryGetComponent(out IconSmoothComponent? protoSmooth, _componentFactory))
                {
                    origSmooth.StateBase = protoSmooth.StateBase;

                    var tempUid = Spawn(prototype);
                    SpriteSystem.CopySprite(tempUid, uid);
                    QueueDel(tempUid);

                    _iconSmooth.DirtyNeighbours(uid);
                }
            }
        }
    }
}
