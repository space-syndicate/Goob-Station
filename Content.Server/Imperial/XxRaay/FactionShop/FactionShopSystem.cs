using Content.Server.Chat.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Imperial.XxRaay.FactionShop;
using Content.Shared.Imperial.XxRaay.FlagSystem;
using Content.Shared.Interaction.Events;
using Content.Shared.NPC.Components;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Localization;
using System.Linq;

namespace Content.Server.Imperial.XxRaay.FactionShop;

/// <summary>
/// Система управления магазинами фракций
/// </summary>
public sealed class FactionShopSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FactionShopComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Начисляем поинты за флаги каждые 20 секунд
        var shopQuery = _entityManager.EntityQuery<FactionShopComponent>();
        foreach (var shop in shopQuery)
        {
            if (_gameTiming.CurTime - shop.LastPointsTime >= TimeSpan.FromSeconds(shop.PointsInterval))
            {
                AwardPointsForFlags(shop);
                shop.LastPointsTime = _gameTiming.CurTime;
            }
        }
    }

    private void OnActivatableUIOpenAttempt(EntityUid uid, FactionShopComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancel();

        // Открываем UI магазина используя стандартную систему
        var playerName = MetaData(args.User).EntityName;
        _chatSystem.TrySendInGameICMessage(uid,
            Loc.GetString("faction-shop-opened", ("player", playerName), ("faction", component.Faction)),
            InGameICChatType.Speak, false);

        // Используем стандартную систему магазинов
        if (TryComp<StoreComponent>(uid, out var store))
        {
            _store.ToggleUi(args.User, uid, store);
        }
    }

    private void AwardPointsForFlags(FactionShopComponent shop)
    {
        // Подсчитываем количество флагов данной фракции
        var flagEnumerator = _entityManager.EntityQueryEnumerator<FlagCaptureComponent, TransformComponent>();
        var flagCount = 0;

        while (flagEnumerator.MoveNext(out var uid, out var flag, out var transform))
        {
            // Определяем фракцию флага
            var flagFaction = GetFlagFaction(uid);
            if (flagFaction == shop.Faction)
            {
                flagCount++;
            }
        }

        if (flagCount > 0)
        {
            var pointsToAward = flagCount * shop.PointsPerFlag;

            // Начисляем поинты в Store компонент
            var currencyId = GetCurrencyIdForFaction(shop.Faction);
            if (currencyId != null && TryComp<StoreComponent>(shop.Owner, out var store))
            {
                var currentBalance = store.Balance.GetValueOrDefault(currencyId, 0);
                var newBalance = currentBalance + pointsToAward;
                store.Balance[currencyId] = newBalance;
                Dirty(shop.Owner, store);
            }
        }
    }



    private string GetFlagFaction(EntityUid flagUid)
    {
        // Определяем фракцию флага по его прототипу
        var metaData = _entityManager.GetComponent<MetaDataComponent>(flagUid);
        var prototypeId = metaData.EntityPrototype?.ID ?? "";
        return FactionHelper.GetFlagFaction(prototypeId);
    }

    private string? GetCurrencyIdForFaction(string faction)
    {
        return FactionHelper.GetCurrencyIdForFaction(faction);
    }
}

