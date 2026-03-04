using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.Imperial.XenoGenetics.Genes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Client.Imperial.XenoGenetics.Genes;

public sealed partial class ClientChangeSpriteGeneSystem : EntitySystem
{    
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangePartGeneComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<ChangePartGeneComponent, ComponentShutdown>(OnComponentShutdown);

    }
    private void OnAfterHandleState(EntityUid uid, ChangePartGeneComponent component, AfterAutoHandleStateEvent args)
    {
        if(!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        
        if (component.Sprite is null)
            return;

        var index = _sprite.LayerMapGet((uid, sprite), component.Layer);

        var speciesProto = _prototypeManager.Index(humanoidAppearance.Species);
        var baseSprites = _prototypeManager.Index(speciesProto.SpriteSet);

        foreach(var (key, id) in baseSprites.Sprites)
        {
            if(key == component.Layer)
            {
                var proto = _prototypeManager.Index<HumanoidSpeciesSpriteLayer>(id);
                component.SpriteOrig = proto.BaseSprite;
            }
        }

        _sprite.LayerSetSprite((uid, sprite), index, component.Sprite);
        _sprite.LayerSetVisible((uid, sprite), index, true);
    }
    private void OnComponentShutdown(EntityUid uid, ChangePartGeneComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        
        if (component.SpriteOrig is null)
            return;

        var index = _sprite.LayerMapGet((uid, sprite), component.Layer);
        _sprite.LayerSetSprite((uid, sprite), index, component.SpriteOrig);
    }
    
}
