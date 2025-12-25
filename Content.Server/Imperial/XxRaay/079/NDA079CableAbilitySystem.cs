using Content.Server.Imperial.XxRaay.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Imperial.XxRaay.Nda079.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Nda079;

public sealed class NDA079CableAbilitySystem : SharedNDA079CableAbilitySystem
{
    [Dependency] private readonly AlertEnergySystem _energySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NDA079CableAbilityComponent, NDA079SpawnCableActionEvent>(OnSpawnCableAction);
        SubscribeLocalEvent<NDA079CableAbilityComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<NDA079CableAbilityComponent> entity, ref MindAddedMessage args)
    {
        if (!_mind.TryGetMind(entity.Owner, out var mindId, out _))
            return;

        var mindActionContainer = EnsureComp<ActionsContainerComponent>(mindId);
        var actionProto = entity.Comp.ActionProto;

        if (HasActionInContainer(mindActionContainer, actionProto.ToString()))
            return;

        var action = _actionContainer.AddAction(mindId, actionProto.ToString(), mindActionContainer);
        if (action != null)
        {
            _actions.GrantContainedAction((entity.Owner, null), (mindId, mindActionContainer), action.Value);
        }
    }

    private bool HasActionInContainer(ActionsContainerComponent container, string actionProto)
    {
        foreach (var action in container.Container.ContainedEntities)
        {
            var protoId = MetaData(action).EntityPrototype?.ID;
            if (protoId != null && protoId.Equals(actionProto))
                return true;
        }

        return false;
    }

    private void OnSpawnCableAction(Entity<NDA079CableAbilityComponent> entity, ref NDA079SpawnCableActionEvent ev)
    {
        var user = entity.Owner;
        var abilityComp = entity.Comp;

        if (!TryComp<AlertEnergyComponent>(user, out var energyComp))
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-notenergy"), user, user);
            return;
        }

        if (energyComp.Energy < abilityComp.EnergyCost)
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-energyfailed",
                ("EnergyCost", abilityComp.EnergyCost),
                ("Energy", energyComp.Energy.ToString("F1"))),
                user,
                user);
            return;
        }

        var curTime = _gameTiming.CurTime;
        if (abilityComp.LastUsedTime.HasValue)
        {
            var timeSinceLastUse = curTime - abilityComp.LastUsedTime.Value;
            var cooldown = TimeSpan.FromSeconds(abilityComp.CooldownSeconds);
            if (timeSinceLastUse < cooldown)
            {
                var remaining = cooldown - timeSinceLastUse;
                _popup.PopupEntity(Loc.GetString("nda079-ability-cooldown",
                    ("remaining", remaining.TotalSeconds.ToString("F1"))),
                    user,
                    user);
                return;
            }
        }

        _energySystem.ModifyEnergy(user, -abilityComp.EnergyCost, energyComp);
        abilityComp.LastUsedTime = curTime;
        Dirty(user, abilityComp);

        var transform = Transform(user);
        var userCoords = transform.Coordinates;
        SpawnAtPosition(abilityComp.CableProto, userCoords);

        _popup.PopupEntity(Loc.GetString("nda079-ability-cable-success"), user, user);
    }
}

