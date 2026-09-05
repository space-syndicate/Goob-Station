using Content.Shared.Examine;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared._CorvaxGoob.SprayableWall;

public sealed class SprayableWallSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayableWallComponent, EntityPaintedEvent>(OnPainted);
    }

    private void OnPainted(Entity<SprayableWallComponent> entity, ref EntityPaintedEvent ev)
    {
        if (!_proto.Resolve(ev.Prototype, out var proto))
            return;

        _meta.SetEntityName(entity, proto.Name);
        _meta.SetEntityDescription(entity, proto.Description);

        if (!TryComp<PaintedComponent>(entity, out var painted))
            return;

        painted.AlwaysShowDetailPainted = true;
    }


}
