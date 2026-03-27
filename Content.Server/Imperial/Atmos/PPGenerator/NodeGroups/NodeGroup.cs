using System.Linq;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Imperial.Power.Generation.PPG;

[NodeGroup(NodeGroupID.PPG)]
public sealed class PPGNodeGroup : BaseNodeGroup
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsFullyBuilt { get; private set; }
    [ViewVariables(VVAccess.ReadWrite)]
    public PPGNodeGenerator? Generator { get; private set; }
    [ViewVariables(VVAccess.ReadWrite)]
    public PPGNodeCirculator? CirculatorA { get; set; }
    [ViewVariables(VVAccess.ReadWrite)]
    public PPGNodeCirculator? CirculatorB { get; set; }
    private IEntityManager? _entityManager;
    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        _entityManager = entMan;
    }
    public override void LoadNodes(List<Node> groupNodes)
    {
        DebugTools.Assert(_entityManager != null);

        base.LoadNodes(groupNodes);

        if (groupNodes.Count > 3)
        {
            return;
        }

        Generator = groupNodes.OfType<PPGNodeGenerator>().SingleOrDefault();
        if (Generator != null)
        {
            // If we have a generator, we can assign CirculatorA and CirculatorB based on relative rotation.
            var xformGenerator = _entityManager.GetComponent<TransformComponent>(Generator.Owner);
            var genDir = xformGenerator.LocalRotation.GetDir();

            foreach (var node in groupNodes)
            {
                if (node is not PPGNodeCirculator circulator)
                    continue;

                var xform = _entityManager.GetComponent<TransformComponent>(node.Owner);
                var dir = xform.LocalRotation.GetDir();
                if (genDir.GetClockwise90Degrees() == dir)
                {
                    CirculatorA = circulator;
                }
                else
                {
                    CirculatorB = circulator;
                }
            }

        }
        if (Generator != null && CirculatorA != null && CirculatorB != null && CirculatorA.NodeGroup != null && CirculatorB.NodeGroup != null)
            IsFullyBuilt = true;
        else
            IsFullyBuilt = false;

        var ppgSystem = _entityManager.EntitySysManager.GetEntitySystem<PPGSystem>();
        foreach (var node in groupNodes)
        {
            if (node is PPGNodeGenerator generator)
                ppgSystem.UpdateGeneratorConnectivity(generator.Owner, this);

            if (node is PPGNodeCirculator circulator)
                ppgSystem.UpdateCirculatorConnectivity(circulator.Owner, this);
        }
    }
}

[DataDefinition]
public sealed partial class PPGNodeGenerator : Node
{
    public override IEnumerable<Node> GetReachableNodes(
        TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        var dir = xform.LocalRotation.GetDir();
        var a = FindCirculator(dir);
        var b = FindCirculator(dir.GetOpposite());

        if (a != null)
            yield return a;

        if (b != null)
            yield return b;

        PPGNodeCirculator? FindCirculator(Direction searchDir)
        {
            var targetIdx = gridIndex.Offset(searchDir);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, targetIdx))
            {
                if (node is not PPGNodeCirculator circulator)
                    continue;

                var entity = node.Owner;
                var entityXform = xformQuery.GetComponent(entity);
                var entityDir = entityXform.LocalRotation.GetDir();

                if (entityDir == searchDir.GetClockwise90Degrees())
                    return circulator;
            }

            return null;
        }
    }
}

[DataDefinition]
public sealed partial class PPGNodeCirculator : Node
{
    public override IEnumerable<Node> GetReachableNodes(
        TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        var dir = xform.LocalRotation.GetDir();
        var searchDir = dir.GetClockwise90Degrees();
        var targetIdx = gridIndex.Offset(searchDir);

        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, targetIdx))
        {
            if (node is not PPGNodeGenerator generator)
                continue;

            var entity = node.Owner;
            var entityXform = xformQuery.GetComponent(entity);
            var entityDir = entityXform.LocalRotation.GetDir();

            if (entityDir == searchDir || entityDir == searchDir.GetOpposite())
            {
                yield return generator;
                break;
            }
        }
    }
}
