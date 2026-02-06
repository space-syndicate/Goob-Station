using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.Imperial.XenoGenetics.Genes;
using Content.Shared.Humanoid;


namespace Content.Client.Imperial.XenoGenetics.Genes;

public sealed partial class ClientAddSpriteGeneSystem : EntitySystem
{    
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddSpriteGeneComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<AddSpriteGeneComponent, ComponentShutdown>(OnComponentShutdown);

    }
    private void OnAfterHandleState(EntityUid uid, AddSpriteGeneComponent component, AfterAutoHandleStateEvent args)
    {
        if(!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        
        if (component.sprite is null)
            return;

        var index = _sprite.LayerMapReserve((uid, sprite), "xeno_gene_layer");
        _sprite.LayerSetSprite((uid, sprite), index, component.sprite);
        _sprite.LayerSetVisible((uid, sprite), index, true);
    }
    private void OnComponentShutdown(EntityUid uid, AddSpriteGeneComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        var index = _sprite.LayerMapGet((uid, sprite), "xeno_gene_layer");
        _sprite.LayerSetVisible((uid, sprite), index, false);
    }
    
}
