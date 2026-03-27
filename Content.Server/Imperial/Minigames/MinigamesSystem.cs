using System.Linq;
using Content.Shared.Imperial.Minigames;
using Content.Shared.Imperial.Minigames.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Minigames;


public sealed class MinigamesSystem : SharedMinigamesSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<InMinigameComponent>();
        var toRemove = new HashSet<(EntityUid, MinigameData)>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (component.ActiveMinigame == null) continue;
            if (component.ActiveMinigame.MaxMinigamePlaytime == null) continue;

            if (component.ActiveMinigame.MaxMinigamePlaytime + component.ActiveMinigame.MinigameStartTime > _timing.CurTime) continue;

            foreach (var player in component.OtherPlayers)
            {
                toRemove.Add((player, component.ActiveMinigame));
            }
        }

        foreach (var rem in toRemove)
        {
            RaiseLocalEvent(rem.Item1, new LoseInMinigameEvent()
            {
                Minigame = rem.Item2
            });

            RemoveMinigame(rem.Item1);
        }
    }

    #region Public API

    public override void AddWonMinigame<T>(EntityUid player, MinigameData minigame, InMinigameComponent? component = null)
    {
        if (!Resolve(player, ref component)) return;
        if (component.ActiveMinigame == null) return;

        var minigameWonPrecent = 1 / component.ActiveMinigame.Minigames.Count + component.MinigameWonPrecent;

        if (minigameWonPrecent >= component.ActiveMinigame.MinMinigamePrecentToWon)
        {
            SetWinner(player, minigame);

            return;
        }

        RemComp<T>(player);
    }

    #region Minigames Start

    public override bool TryStartMinigame(EntityUid player, string minigameId)
    {
        if (!_prototypeManager.TryIndex<MinigamePrototype>(minigameId, out var minigamePrototype)) return false;

        return TryStartMinigame(player, minigamePrototype);
    }

    public override bool TryStartMinigame(EntityUid player, MinigamePrototype minigamePrototype)
    {
        if (!CheckCanAddMinigame(player, minigamePrototype)) return false;
        if (!CheckBlackListComponent(player, minigamePrototype.ComponentBlackList)) return false;
        if (!PassesMinigamePrerequisites(player, minigamePrototype)) return false;

        AddComponentsToTarget(player, minigamePrototype.Minigames);
        AddMinigame(player, minigamePrototype);

        RaiseLocalEvent(player, new AfterMinigameAddedEvent
        {
            NewPlayer = player,
            MinigamePrototype = minigamePrototype.ID
        });

        if (!minigamePrototype.StartInstantly) return true;

        RaiseNetworkEvent(new StartMinigameEvent()
        {
            Player = GetNetEntity(player),
            MinigamePrototype = minigamePrototype.ID
        });

        return true;
    }

    public override bool TryStartMinigameBetween(EntityUid player, EntityUid player2, string minigameId)
    {
        if (!_prototypeManager.TryIndex<MinigamePrototype>(minigameId, out var minigamePrototype)) return false;

        return TryStartMinigameBetween(player, player2, minigamePrototype);
    }

    public override bool TryStartMinigameBetween(EntityUid player, EntityUid player2, MinigamePrototype minigamePrototype)
    {
        if (HasComp<InMinigameComponent>(player)) return false;
        if (HasComp<InMinigameComponent>(player2)) return false;

        if (!TryStartMinigame(player, minigamePrototype)) return false;
        if (!TryStartMinigame(player2, minigamePrototype))
        {
            RemoveMinigame(player);

            return false;
        }

        EnsureComp<InMinigameComponent>(player).OtherPlayers.Add(player2);
        EnsureComp<InMinigameComponent>(player2).OtherPlayers.Add(player);

        return true;
    }

    public override bool TryStartMinigameBetween(List<EntityUid> players, string minigameId)
    {
        if (!_prototypeManager.TryIndex<MinigamePrototype>(minigameId, out var minigamePrototype)) return false;

        return TryStartMinigameBetween(players, minigamePrototype);
    }

    public override bool TryStartMinigameBetween(List<EntityUid> players, MinigamePrototype minigamePrototype)
    {
        var allValid = true;

        foreach (var player in players)
        {
            if (!TryStartMinigame(player, minigamePrototype))
            {
                EnsureComp<InMinigameComponent>(player).OtherPlayers = players.Where(uid => uid != player).ToList();

                continue;
            }

            allValid = false;

            break;
        }

        if (!allValid)
            foreach (var player in players)
                if (HasComp<InMinigameComponent>(player)) RemoveMinigame(player);

        return allValid;
    }

    #endregion

    #endregion

    #region Helpers

    private void AddComponentsToTarget(EntityUid target, ComponentRegistry components)
    {
        foreach (var componentRegistryEntry in components.Values)
        {
            if (componentRegistryEntry.Component is not MinigameComponent) continue;
            var minigameComponent = _serializationManager.CreateCopy(componentRegistryEntry.Component, notNullableOverride: true);

            AddComp(target, minigameComponent, true);
        }
    }

    private void AddMinigame(EntityUid target, MinigameData minigame)
    {
        var minigameComponent = EnsureComp<InMinigameComponent>(target);
        var minigameClone = (MinigameData)minigame.Clone();

        minigameClone.MinigameStartTime = _timing.CurTime;

        minigameComponent.ActiveMinigame = minigameClone;
        minigameComponent.FirstMinigameStartTime = minigameComponent.FirstMinigameStartTime == TimeSpan.Zero
            ? _timing.CurTime
            : minigameComponent.FirstMinigameStartTime;
    }

    private void SetWinner(EntityUid player, MinigameData minigame, InMinigameComponent? component = null)
    {
        if (!Resolve(player, ref component)) return;

        if (minigame.OnlyOneWinner)
        {
            foreach (var otherPlayer in component.OtherPlayers)
            {
                RaiseLocalEvent(otherPlayer, new LoseInMinigameEvent()
                {
                    Minigame = minigame
                });

                RemoveMinigame(otherPlayer);
            }
        }

        RaiseLocalEvent(player, new WinInMinigamEvent()
        {
            Minigame = minigame
        });

        RemoveMinigame(player);
    }

    private void RemoveMinigame(EntityUid player, InMinigameComponent? component = null)
    {
        if (!Resolve(player, ref component)) return;
        if (component.ActiveMinigame == null) return;

        foreach (var componentRegistryEntry in component.ActiveMinigame.Minigames.Values)
        {
            if (componentRegistryEntry.Component is not MinigameComponent) continue;
            var toRemove = componentRegistryEntry.Component.GetType();

            if (!HasComp(player, toRemove)) continue;

            RemComp(player, toRemove);
        }

        RemComp<InMinigameComponent>(player);
    }

    #region Checks

    private bool CheckBlackListComponent(EntityUid target, ComponentRegistry components)
    {
        foreach (var componentID in components.Keys)
        {
            if (!components.TryGetComponent(componentID, out var component)) continue;
            if (!HasComp(target, component.GetType())) continue;

            return false;
        }

        return true;
    }

    private bool CheckCanAddMinigame(EntityUid target, MinigameData minigame)
    {
        if (!TryComp<InMinigameComponent>(target, out var inMinigameComponent)) return true;
        if (inMinigameComponent.ActiveMinigame != null) return false;

        return true;
    }

    #endregion

    #region Events Helpers

    private bool PassesMinigamePrerequisites(EntityUid player, MinigamePrototype minigame)
    {
        var ev = new BeforeMinigameAddedEvent()
        {
            NewPlayer = player,
            MinigamePrototype = minigame.ID
        };
        RaiseLocalEvent(player, ev);

        return !ev.Cancel;
    }

    #endregion

    #endregion
}
