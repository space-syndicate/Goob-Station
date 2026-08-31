// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.PDA;
using Content.Shared._CorvaxGoob.CardPdaPainter;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CorvaxGoob.CardPdaPainter;

public sealed class CardPdaVisualOverrideSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardPdaVisualOverrideComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<CardPdaVisualOverrideComponent, ComponentStartup>(OnStartup);
    }

    private void OnHandleState(Entity<CardPdaVisualOverrideComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnStartup(Entity<CardPdaVisualOverrideComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<CardPdaVisualOverrideComponent> ent)
    {
        if (ent.Comp.VisualPrototype is not { } visualPrototype ||
            !_prototype.TryIndex<EntityPrototype>(visualPrototype, out var prototype))
        {
            return;
        }

        // Sprite data is client-side, so the server stores only the template prototype ID.
        if (TryComp<SpriteComponent>(ent, out var sprite) &&
            prototype.TryGetComponent<SpriteComponent>(out var otherSprite, Factory))
        {
            sprite.CopyFrom(otherSprite);
        }

        // PDA windows use these colors for their frame, so copy them along with the world sprite.
        if (TryComp<PdaBorderColorComponent>(ent, out var borderColor) &&
            prototype.TryGetComponent<PdaBorderColorComponent>(out var otherBorderColor, Factory))
        {
            borderColor.BorderColor = otherBorderColor.BorderColor;
            borderColor.AccentHColor = otherBorderColor.AccentHColor;
            borderColor.AccentVColor = otherBorderColor.AccentVColor;
        }
    }
}
