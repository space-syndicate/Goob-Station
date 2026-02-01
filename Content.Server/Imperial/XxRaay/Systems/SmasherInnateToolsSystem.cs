using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Interaction.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// Система для автоматического добавления встроенных инструментов смешера в руки.
/// </summary>
public sealed class SmasherInnateToolsSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmasherInnateToolsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SmasherInnateToolsComponent component, MapInitEvent args)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        var spawnCoord = Transform(uid).Coordinates;
        var handIndex = 0;

        foreach (var toolProtoId in component.Tools)
        {
            if (handIndex >= component.Hands.Count)
                break;

            if (!_prototypeManager.TryIndex<EntityPrototype>(toolProtoId, out var prototype))
                continue;

            var handId = component.Hands[handIndex];
            
            if (!_handsSystem.TryGetHand((uid, hands), handId, out _))
            {
                handIndex++;
                continue;
            }

            if (!_handsSystem.HandIsEmpty((uid, hands), handId))
            {
                handIndex++;
                continue;
            }

            var item = Spawn(toolProtoId, spawnCoord);
            AddComp<UnremoveableComponent>(item);

            if (_handsSystem.TryPickup(uid, item, handId, checkActionBlocker: false, handsComp: hands))
            {
                component.ToolUids.Add(item);
            }
            else
            {
                QueueDel(item);
            }

            handIndex++;
        }
    }
}

