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
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Nda079;

public sealed class NDA079LightFlickerAbilitySystem : SharedNDA079LightFlickerAbilitySystem
{
    [Dependency] private readonly AlertEnergySystem _energySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly NDA079CpuSystem _cpuSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NDA079LightFlickerAbilityComponent, NDA079LightFlickerActionEvent>(OnLightFlickerAction);
        SubscribeLocalEvent<NDA079LightFlickerAbilityComponent, MindAddedMessage>(OnMindAdded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NDA079LightRestoreTimerComponent>();
        var curTime = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out var timer))
        {
            if (curTime < timer.RestoreTime)
                continue;

            if (_lightSystem.TryGetLight(uid, out var lightComp))
            {
                _lightSystem.SetEnabled(uid, timer.WasEnabled, lightComp);
            }

            RemComp<NDA079LightRestoreTimerComponent>(uid);
        }
    }

    private void OnMindAdded(Entity<NDA079LightFlickerAbilityComponent> entity, ref MindAddedMessage args)
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

    private void OnLightFlickerAction(Entity<NDA079LightFlickerAbilityComponent> entity,
        ref NDA079LightFlickerActionEvent ev)
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
            if (timeSinceLastUse < abilityComp.Cooldown)
            {
                var remaining = abilityComp.Cooldown - timeSinceLastUse;
                _popup.PopupEntity(Loc.GetString("nda079-ability-cooldown",
                        ("remaining", remaining.TotalSeconds.ToString("F1"))),
                    user,
                    user);
                return;
            }
        }

        if (!TryComp<TransformComponent>(user, out var transform))
            return;

        if (transform.MapID == MapId.Nullspace)
            return;

        _energySystem.ModifyEnergy(user, -abilityComp.EnergyCost, energyComp);
        abilityComp.LastUsedTime = curTime;
        Dirty(user, abilityComp);

        _cpuSystem.AddCpuPoint(user);

        if (TryComp<NDA079Component>(user, out var nda079Comp))
        {
            nda079Comp.LightFlickerLastUsedTime = curTime;
            Dirty(user, nda079Comp);
        }

        var userCoords = transform.Coordinates;
        var entitiesInRange = _lookup.GetEntitiesInRange(userCoords, abilityComp.Radius);

        var lightsFound = new List<EntityUid>();
        foreach (var entityInRange in entitiesInRange)
        {
            if (_lightSystem.TryGetLight(entityInRange, out _))
            {
                lightsFound.Add(entityInRange);
            }
        }

        if (lightsFound.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-light-notlamp"), user, user);
            return;
        }

        var success = _random.Prob(abilityComp.SuccessChance);

        if (success)
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-light-success"), user, user);

            foreach (var lightEntity in lightsFound)
            {
                if (!_lightSystem.TryGetLight(lightEntity, out var lightComp))
                    continue;

                var wasEnabled = lightComp.Enabled;
                _lightSystem.SetEnabled(lightEntity, false, lightComp);

                var timer = EnsureComp<NDA079LightRestoreTimerComponent>(lightEntity);
                timer.RestoreTime = curTime + abilityComp.LightOffDuration;
                timer.WasEnabled = wasEnabled;
                Dirty(lightEntity, timer);
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-failed"), user, user);

            foreach (var lightEntity in lightsFound)
            {
                if (!_lightSystem.TryGetLight(lightEntity, out var lightComp))
                    continue;

                var wasEnabled = lightComp.Enabled;
                _lightSystem.SetEnabled(lightEntity, false, lightComp);

                var timer = EnsureComp<NDA079LightRestoreTimerComponent>(lightEntity);
                timer.RestoreTime = curTime + abilityComp.FlickerDuration;
                timer.WasEnabled = wasEnabled;
                Dirty(lightEntity, timer);
            }
        }
    }
}

