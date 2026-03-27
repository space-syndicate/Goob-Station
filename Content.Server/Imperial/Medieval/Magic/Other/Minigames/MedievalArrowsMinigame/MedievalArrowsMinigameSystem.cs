using Content.Server.Imperial.Minigames;
using Content.Shared.Imperial.Medieval.Magic.Minigames;
using Content.Shared.Imperial.Medieval.Magic.Minigames.Events;
using Content.Shared.Imperial.Minigames.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Imperial.Medieval.Magic.Minigames;


public sealed partial class MedievalArrowsMinigameSystem : SharedMedievalArrowsMinigameSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MinigamesSystem _minigamesSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MedievalArrowValidCombination>(OnValidCombination);
        SubscribeNetworkEvent<MedievalArrowInvalidCombination>(OnInvalidCombination);


        SubscribeLocalEvent<MedievalArrowsMinigameComponent, AfterMinigameAddedEvent>(OnAdded);
    }

    private void OnValidCombination(MedievalArrowValidCombination args)
    {
        var player = GetEntity(args.Player);

        if (!TryComp<MedievalArrowsMinigameComponent>(player, out var component)) return;

        _minigamesSystem.AddWonMinigame<MedievalArrowsMinigameComponent>(player, component.CurrentMinigame);
    }

    private void OnInvalidCombination(MedievalArrowInvalidCombination args)
    {
        var player = GetEntity(args.Player);

        if (!TryComp<MedievalArrowsMinigameComponent>(player, out var component)) return;
    }

    private void OnAdded(EntityUid uid, MedievalArrowsMinigameComponent component, AfterMinigameAddedEvent args)
    {
        if (!_prototypeManager.TryIndex(args.MinigamePrototype, out var minigame)) return;

        component.CurrentMinigame = minigame;

        if (component.Combination.Count != 0) return;

        var additionalArrows = Math.Floor((component.Difficulty - 1.0f) / component.ArrowPerDifficulty);
        var availableArrows = new List<ArrowsTypes>() { ArrowsTypes.ArrowUp, ArrowsTypes.ArrowLeft, ArrowsTypes.ArrowRight, ArrowsTypes.ArrowDown };

        if (component.BaseArrowsCount + additionalArrows <= 0) return;

        for (var i = 0; i < component.BaseArrowsCount + additionalArrows; i++)
            component.Combination.Add(_random.Pick(availableArrows));

        Dirty(uid, component);
    }
}
