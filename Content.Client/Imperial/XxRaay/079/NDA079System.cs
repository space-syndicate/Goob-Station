using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.StationAi;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Imperial.XxRaay.Nda079;

public sealed class NDA079System : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NDA079Component, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NDA079Component, AfterAutoHandleStateEvent>(OnComponentState);
    }

    private void OnComponentStartup(EntityUid uid, NDA079Component component, ComponentStartup args)
    {
        UpdateSprite(uid, component);
    }

    private void OnComponentState(EntityUid uid, NDA079Component component, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(uid, component);
    }

    private void UpdateSprite(EntityUid uid, NDA079Component component)
    {
        // Обновляем спрайт только для оригинальной сущности, не для летающей
        // Летающая сущность имеет StationAiVisionComponent
        if (HasComp<StationAiVisionComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Проверяем, что у спрайта есть нужные состояния (ai-on и ai-off)
        var rsi = sprite.BaseRSI;
        if (rsi == null || !rsi.TryGetState("ai-on", out _) || !rsi.TryGetState("ai-off", out _))
            return;

        var state = component.InAIVisionMode ? "ai-off" : "ai-on";
        _sprite.LayerSetRsiState((uid, sprite), 0, state);
    }
}
