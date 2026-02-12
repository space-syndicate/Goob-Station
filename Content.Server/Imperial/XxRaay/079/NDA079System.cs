using Content.Server.Mind;
using Content.Server.Speech.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Imperial.XxRaay.Nda079.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;
using Content.Server.Imperial.XxRaay.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Nda079;

public sealed class NDA079System : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedMindSystem _sharedMind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpeechSystem _speech = default!;
    [Dependency] private readonly AlertEnergySystem _energySystem = default!;
    [Dependency] private readonly NDA079GeneratorSystem _generatorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NDA079Component, NDA079ToggleVisionModeEvent>(OnToggleVisionMode);
        SubscribeLocalEvent<NDA079Component, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, NDA079Component comp, ref MindAddedMessage args)
    {
        if (!_sharedMind.TryGetMind(uid, out var mindId, out _))
            return;

        var action = EnsureToggleActionInMind(mindId, comp.ToggleActionProto);
        GrantActionIfPresent(uid, mindId, action);
    }

    private void OnToggleVisionMode(EntityUid uid, NDA079Component comp, ref NDA079ToggleVisionModeEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.InAIVisionMode)
        {
            if (!_sharedMind.TryGetMind(uid, out var mindId, out var mind))
                return;

            var userId = mind.UserId;
            if (userId == null)
                return;

            var proto = comp.AIVisionFlyingEntityProto;
            var coords = Transform(uid).Coordinates;
            var newb = SpawnAtPosition(proto, coords);
            var newcomp = EnsureComp<NDA079Component>(newb);

            newcomp.InAIVisionMode = true;
            newcomp.OriginalEntity = uid;
            newcomp.AIVisionEntity = null;
            newcomp.OriginalEntityProto = MetaData(uid).EntityPrototype?.ID;
            Dirty(newb, newcomp);

            comp.InAIVisionMode = true;
            comp.AIVisionEntity = newb;
            comp.OriginalEntity = uid;
            comp.OriginalEntityProto = MetaData(uid).EntityPrototype?.ID;
            Dirty(uid, comp);

            EnsureComp<SpeechComponent>(newb);
            _speech.SetSpeech(newb, true);

            if (TryComp<AlertEnergyComponent>(uid, out var sourceEnergy))
            {
                if (TryComp<AlertEnergyComponent>(newb, out var targetEnergy))
                {
                    _energySystem.SetEnergy(newb, sourceEnergy.Energy, targetEnergy);
                }
            }

            if (TryComp<NDA079CpuComponent>(uid, out var originalCpu))
            {
                var newCpu = EnsureComp<NDA079CpuComponent>(newb);
                newCpu.CurrentLevel = originalCpu.CurrentLevel;
                newCpu.CurrentCpu = originalCpu.CurrentCpu;
                Dirty(newb, newCpu);
            }

            if (TryComp<NDA079CpuComponent>(uid, out var cpu))
            {
                var level = cpu.CurrentLevel;

                if (TryComp<NDA079LightFlickerAbilityComponent>(newb, out var lightFlickerComp))
                {
                    lightFlickerComp.LastUsedTime = comp.LightFlickerLastUsedTime;

                    if (_prototype.TryIndex<NDA079LightFlickerLevelPrototype>($"NDA079LightFlickerLevel{level}", out var lightProto))
                    {
                        lightFlickerComp.LightOffDuration = lightProto.LightOffDuration;
                        lightFlickerComp.Radius = lightProto.Radius;
                        lightFlickerComp.SuccessChance = lightProto.SuccessChance;
                        lightFlickerComp.Cooldown = lightProto.Cooldown;
                    }

                    Dirty(newb, lightFlickerComp);
                }

                if (TryComp<NDA079AirlockAbilityComponent>(newb, out var airlockComp))
                {
                    airlockComp.LastUsedTime = comp.AirlockAbilityLastUsedTime;

                    if (_prototype.TryIndex<NDA079AirlockLevelPrototype>($"NDA079AirlockLevel{level}", out var airlockProto))
                    {
                        airlockComp.BoltDuration = airlockProto.BoltDuration;
                        airlockComp.SuccessChance = airlockProto.SuccessChance;
                    }

                    Dirty(newb, airlockComp);
                }
            }

            _actions.RemoveProvidedActions(uid, mindId);

            _mind.WipeMind(mindId);
            var newMind = _mind.CreateMind(userId.Value, MetaData(uid).EntityName);
            _sharedMind.SetUserId(newMind, userId.Value);

            _mind.TransferTo(newMind, newb);
            var action = EnsureToggleActionInMind(newMind, comp.ToggleActionProto);
            GrantActionIfPresent(newb, newMind, action);

            if (TryComp<NDA079LightFlickerAbilityComponent>(newb, out var newLightFlickerComp))
            {
                var protoId = newLightFlickerComp.ActionProto.ToString();
                var lightFlickerAction = EnsureActionInMind(newMind, protoId);
                GrantActionIfPresent(newb, newMind, lightFlickerAction);
            }

            _generatorSystem.RefreshRegen(newb);
        }
        else
        {
            if (!_sharedMind.TryGetMind(uid, out var mindId, out var mind))
                return;

            var userId = mind.UserId;
            if (userId == null)
                return;

            var originalEntity = comp.OriginalEntity;
            if (originalEntity == null || !Exists(originalEntity.Value))
                return;

            var originalEntityValue = originalEntity.Value;

            if (!TryComp<NDA079Component>(originalEntityValue, out var originalComp))
                return;

            originalComp.InAIVisionMode = false;
            originalComp.AIVisionEntity = null;
            originalComp.OriginalEntity = null;
            Dirty(originalEntityValue, originalComp);

            RemComp<MutedComponent>(originalEntityValue);
            RemComp<BlockListeningComponent>(originalEntityValue);
            _speech.SetSpeech(originalEntityValue, true);

            if (TryComp<AlertEnergyComponent>(uid, out var sourceEnergy))
            {
                if (TryComp<AlertEnergyComponent>(originalEntityValue, out var targetEnergy))
                {
                    _energySystem.SetEnergy(originalEntityValue, sourceEnergy.Energy, targetEnergy);
                }
            }

            if (TryComp<NDA079CpuComponent>(uid, out var flyingCpu))
            {
                var statCpu = EnsureComp<NDA079CpuComponent>(originalEntityValue);
                statCpu.CurrentLevel = flyingCpu.CurrentLevel;
                statCpu.CurrentCpu = flyingCpu.CurrentCpu;
                Dirty(originalEntityValue, statCpu);
            }

            if (TryComp<NDA079LightFlickerAbilityComponent>(uid, out var lightFlickerComp))
            {
                originalComp.LightFlickerLastUsedTime = lightFlickerComp.LastUsedTime;
                Dirty(originalEntityValue, originalComp);
            }

            if (TryComp<NDA079AirlockAbilityComponent>(uid, out var airlockComp))
            {
                originalComp.AirlockAbilityLastUsedTime = airlockComp.LastUsedTime;
                Dirty(originalEntityValue, originalComp);
            }

            _actions.RemoveProvidedActions(uid, mindId);

            _mind.WipeMind(mindId);
            var newMind = _mind.CreateMind(userId.Value, MetaData(originalEntityValue).EntityName);
            _sharedMind.SetUserId(newMind, userId.Value);

            _mind.TransferTo(newMind, originalEntityValue);
            var action = EnsureToggleActionInMind(newMind, originalComp.ToggleActionProto);
            GrantActionIfPresent(originalEntityValue, newMind, action);

            Del(uid);

            _generatorSystem.RefreshRegen(originalEntityValue);
        }

        args.Handled = true;
    }

    private EntityUid? EnsureToggleActionInMind(EntityUid mindId, EntProtoId actionProto)
    {
        var mindActionContainer = EnsureComp<ActionsContainerComponent>(mindId);
        if (HasActionInContainer(mindActionContainer, actionProto))
            return null;

        return _actionContainer.AddAction(mindId, actionProto.ToString(), mindActionContainer);
    }

    private EntityUid? EnsureActionInMind(EntityUid mindId, string actionProto)
    {
        var mindActionContainer = EnsureComp<ActionsContainerComponent>(mindId);
        if (HasActionInContainer(mindActionContainer, actionProto))
            return null;

        return _actionContainer.AddAction(mindId, actionProto, mindActionContainer);
    }

    private bool HasActionInContainer(ActionsContainerComponent container, EntProtoId actionProto)
    {
        return HasActionInContainer(container, actionProto.ToString());
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

    private void GrantActionIfPresent(EntityUid performer, EntityUid mindId, EntityUid? action)
    {
        if (action == null)
            return;

        var container = EnsureComp<ActionsContainerComponent>(mindId);
        _actions.GrantContainedAction((performer, null), (mindId, container), action.Value);
    }
}
