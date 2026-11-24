using Content.Shared._CorvaxGoob.Appearance;
using Content.Shared.Alert;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Client._CorvaxGoob.Appearance;

public sealed partial class CopySpriteAppearanceSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CopySpriteAppearanceComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CopySpriteAppearanceComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is null)
            return;

        if (args.TryGetData<NetEntity>(CopySpriteAppearance.NetEntity, out var netEntity))
        {
            var source = GetEntity(netEntity);

            if (!TryComp<SpriteComponent>(source, out var sourceSprite))
                return;

            _sprite.CopySprite((source, sourceSprite), (entity, args.Sprite));
        }

        if (args.TryGetData<bool>(CopySpriteAppearance.NoRot, out var noRot))
            args.Sprite.NoRotation = noRot;
    }
}
