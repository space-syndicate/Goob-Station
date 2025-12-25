using Content.Server.Imperial.XxRaay.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.NodeContainer;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Nda079;

public sealed class NDA079GeneratorSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly AlertEnergySystem _energySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NDA079Component, ComponentStartup>(OnNDA079Startup);
        SubscribeLocalEvent<NDA079GeneratorComponent, ComponentStartup>(OnGeneratorStartup);
        SubscribeLocalEvent<NDA079GeneratorComponent, ComponentShutdown>(OnGeneratorShutdown);
        SubscribeLocalEvent<NodeContainerComponent, NodeGroupsRebuilt>(OnNodeGroupsRebuilt);
    }

    private void OnNDA079Startup(Entity<NDA079Component> entity, ref ComponentStartup args)
    {
        UpdateGeneratorRegen(entity.Owner);
    }

    private void OnGeneratorStartup(Entity<NDA079GeneratorComponent> entity, ref ComponentStartup args)
    {
        UpdateAllNDA079Regen();
    }

    private void OnGeneratorShutdown(Entity<NDA079GeneratorComponent> entity, ref ComponentShutdown args)
    {
        UpdateAllNDA079Regen();
    }

    private void OnNodeGroupsRebuilt(Entity<NodeContainerComponent> entity, ref NodeGroupsRebuilt args)
    {
        if (HasComp<NDA079Component>(entity.Owner) || HasComp<NDA079GeneratorComponent>(entity.Owner))
        {
            UpdateAllNDA079Regen();
        }
    }

    public void RefreshRegen(EntityUid nda079Entity)
    {
        UpdateGeneratorRegen(nda079Entity);
    }

    private void UpdateAllNDA079Regen()
    {
        var query = EntityQueryEnumerator<NDA079Component>();
        while (query.MoveNext(out var uid, out _))
        {
            UpdateGeneratorRegen(uid);
        }
    }

    private void UpdateGeneratorRegen(EntityUid nda079Entity)
    {
        if (!TryComp<NDA079Component>(nda079Entity, out var nda079Comp))
            return;

        EntityUid? stationaryEntity = null;
        EntityUid? activeEntity = null;

        if (nda079Comp.InAIVisionMode)
        {
            activeEntity = nda079Entity;
            stationaryEntity = nda079Comp.OriginalEntity;
        }
        else
        {
            activeEntity = nda079Entity;
            stationaryEntity = nda079Entity;
        }

        if (activeEntity == null || !Exists(activeEntity.Value))
            return;

        if (stationaryEntity == null || !Exists(stationaryEntity.Value))
        {
            SetBaseRegenForEntity(activeEntity.Value);
            return;
        }

        var totalBonus = GetConnectedGeneratorsBonus(stationaryEntity.Value);
        ApplyRegenBonus(activeEntity.Value, totalBonus);

        if (nda079Comp.InAIVisionMode && nda079Comp.OriginalEntity != null && Exists(nda079Comp.OriginalEntity.Value))
        {
            ApplyRegenBonus(nda079Comp.OriginalEntity.Value, totalBonus);
        }
    }

    private float GetConnectedGeneratorsBonus(EntityUid stationaryEntity)
    {
        if (!TryComp<NodeContainerComponent>(stationaryEntity, out _))
            return 0f;

        if (!_nodeContainer.TryGetNode<CableDeviceNode>(stationaryEntity, "power", out var stationaryNode))
            return 0f;

        var stationaryNodeGroup = stationaryNode.NodeGroup;
        if (stationaryNodeGroup == null)
            return 0f;

        var totalBonus = 0f;
        var generatorQuery = EntityQueryEnumerator<NDA079GeneratorComponent, NodeContainerComponent>();
        while (generatorQuery.MoveNext(out var generatorUid, out var generatorComp, out _))
        {
            if (!_nodeContainer.TryGetNode<CableDeviceNode>(generatorUid, "output", out var generatorNode))
                continue;

            if (generatorNode.NodeGroup == stationaryNodeGroup)
            {
                totalBonus += generatorComp.RegenBonusPerGenerator;
            }
        }

        return totalBonus;
    }

    private void ApplyRegenBonus(EntityUid entity, float bonus)
    {
        if (!TryComp<AlertEnergyComponent>(entity, out var energyComp))
            return;

        var baseRegen = GetBaseRegen(entity, energyComp);
        energyComp.RegenPerSecond = baseRegen + bonus;
        Dirty(entity, energyComp);
    }

    private void SetBaseRegenForEntity(EntityUid entity)
    {
        if (!TryComp<AlertEnergyComponent>(entity, out var energyComp))
            return;

        var baseRegen = GetBaseRegen(entity, energyComp);
        energyComp.RegenPerSecond = baseRegen;
        Dirty(entity, energyComp);
    }

    private float GetBaseRegen(EntityUid uid, AlertEnergyComponent component)
    {
        var proto = MetaData(uid).EntityPrototype;
        if (proto == null)
            return component.RegenPerSecond;

        var compFactory = IoCManager.Resolve<IComponentFactory>();
        var compName = compFactory.GetComponentName<AlertEnergyComponent>();
        if (!proto.Components.TryGetValue(compName, out var entry))
            return component.RegenPerSecond;

        if (entry.Component is not AlertEnergyComponent protoComponent)
            return component.RegenPerSecond;

        return protoComponent.RegenPerSecond;
    }

    private void SetBaseRegen(EntityUid uid, AlertEnergyComponent component)
    {
        var baseRegen = GetBaseRegen(uid, component);
        component.RegenPerSecond = baseRegen;
        Dirty(uid, component);
    }
}
