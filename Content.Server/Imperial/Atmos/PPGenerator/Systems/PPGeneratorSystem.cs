using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.Imperial.Power.Generation.PPG;
using Content.Server.Imperial.Atmos.Reactions.Prototypes;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Power.Generation.PPG;

public sealed class PPGSystem : EntitySystem
{
    private const string NodeNamePPG = "ppg";
    private const string NodeNameInlet = "inlet";
    private const string NodeNameOutlet = "outlet";
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;
    private GasPhazeReactionPrototype[] _gasReactions = Array.Empty<GasPhazeReactionPrototype>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PPGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<PPGeneratorComponent, PowerChangedEvent>(GeneratorPowerChange);

        SubscribeLocalEvent<PPGeneratorComponent, ExaminedEvent>(GeneratorExamined);
        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void GeneratorExamined(EntityUid uid, PPGeneratorComponent component, ExaminedEvent args)
    {
        if (GetNodeGroup(uid) is not { IsFullyBuilt: true })
        {
            args.PushMarkup(Loc.GetString("teg-generator-examine-connection"));
        }
        else
        {
            var supplier = Comp<PowerSupplierComponent>(uid);

            using (args.PushGroup(nameof(PPGeneratorComponent)))
            {
                args.PushMarkup(Loc.GetString("teg-generator-examine-power", ("power", supplier.CurrentSupply)));
                args.PushMarkup(Loc.GetString("teg-generator-examine-power-max-output", ("power", supplier.MaxSupply)));
            }
        }
    }

    public void GeneratorUpdate(EntityUid uid, PPGeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        if (!powerReceiver.Powered)
        {
            supplier.MaxSupply = 0;
            return;
        }

        var ppgGroup = GetNodeGroup(uid);
        if (ppgGroup is not { IsFullyBuilt: true })
            return;

        _gasReactions = _protoMan.EnumeratePrototypes<GasPhazeReactionPrototype>().ToArray();
        var circA = ppgGroup.CirculatorA!.Owner;
        var circB = ppgGroup.CirculatorB!.Owner;
        var (inletA, outletA) = GetPipes(circA);
        var (inletB, outletB) = GetPipes(circB);

        var (airA, δpA) = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var (airB, δpB) = GetCirculatorAirTransfer(inletB.Air, outletB.Air);
        var initACap = airA.Pressure;
        var initBCap = airB.Pressure;
        foreach (var prototype in _gasReactions)
        {
            var initMissingGasIDA = airA.GetMoles(prototype.MissingGasID);
            var initMissingGasIDB = airB.GetMoles(prototype.MissingGasID);
            var initMissingDeuteriumA = airA.GetMoles(Gas.Deuterium);
            var initMissingDeuteriumB = airB.GetMoles(Gas.Deuterium);
            var missingDeuteriumA = component.MissingDeuteriumRate * initMissingDeuteriumA;
            var missingDeuteriumB = component.MissingDeuteriumRate * initMissingDeuteriumB;
            if (initMissingDeuteriumA > 0.1 || initMissingDeuteriumB > 0.1)
            {
                component.DeuteriumReactionActive = true;
                airA.AdjustMoles(Gas.Deuterium, -missingDeuteriumA);
                airB.AdjustMoles(Gas.Deuterium, -missingDeuteriumB);
            }
            component.Deuterium = initMissingDeuteriumA + initMissingDeuteriumB;
            if (prototype.UseGasTwo)
            {
                var initMissingGasIDTwoA = airA.GetMoles(prototype.MissingGasIDTwo);
                var initMissingGasIDTwoB = airB.GetMoles(prototype.MissingGasIDTwo);
                if (initMissingGasIDTwoA > 0.1 || initMissingGasIDTwoB > 0.1)
                {
                    component.SecondaryGasActive = true;
                    airA.AdjustMoles(prototype.MissingGasIDTwo, -initMissingGasIDTwoA);
                    airB.AdjustMoles(prototype.MissingGasIDTwo, -initMissingGasIDTwoB);
                }
                component.InitMissingGasTwo = initMissingGasIDTwoA + initMissingGasIDTwoB;
            }
            if (!prototype.UseGasTwo)
                component.SecondaryGasActive = true;
            if (initMissingGasIDA > 0.1 || initMissingGasIDB > 0.1)
                component.PrimaryGasActive = true;
            if (component.DeuteriumReactionActive && component.SecondaryGasActive && component.PrimaryGasActive)
            {
                airA.AdjustMoles(prototype.MissingGasID, -initMissingGasIDA);
                airB.AdjustMoles(prototype.MissingGasID, -initMissingGasIDB);
                var initMissingGas = initMissingGasIDA + initMissingGasIDB;
                // output gas = Missing Gas moles (Id's in reaction prototype) * Missing Second Gas moles (Id's in reaction prototype) * (OutputFactor in reaction prototype / 10)
                var output = initMissingGas * component.InitMissingGasTwo * (prototype.OutputFactor / 10);
                airA.AdjustMoles(prototype.AddedGasID, output);
                airB.AdjustMoles(prototype.AddedGasID, output);
                // power = EnergyScale in reaction prototype * (Capacity A circulator + Capacity B circulator)
                var power = prototype.EnergyScale * (initACap + initBCap);
                supplier.MaxSupply = power * component.RampFactor;
                if (power > component.MinimumEnergy)
                    component.Active = true;
                if (power < component.MinimumEnergy)
                    component.Active = false;
                UpdateAppearance(uid, component, powerReceiver, ppgGroup);
            }
            if (initACap < component.MinimumPressure && initBCap < component.MinimumPressure)
            {
                component.DeuteriumReactionActive = false;
                component.SecondaryGasActive = false;
                component.PrimaryGasActive = false;
            }
            _atmosphere.Merge(outletA.Air, airA);
            _atmosphere.Merge(outletB.Air, airB);
            continue;
        }
    }

    private void UpdateAppearance(
        EntityUid uid,
        PPGeneratorComponent component,
        ApcPowerReceiverComponent powerReceiver,
        PPGNodeGroup nodeGroup)
    {
        _ambientSound.SetAmbience(uid, component.Active);
        if (component.Active && GetNodeGroup(uid) is { IsFullyBuilt: true })
            component.PowerLevel = 1;
        else
            component.PowerLevel = 0;
        _appearance.SetData(uid, PPGVisualsState.PowerOutput, component.PowerLevel);
    }

    [Access(typeof(PPGNodeGroup))]
    public void UpdateGeneratorConnectivity(
        EntityUid uid,
        PPGNodeGroup group,
        PPGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        _receiver.SetPowerDisabled(uid, !group.IsFullyBuilt, powerReceiver);
        UpdateAppearance(uid, component, powerReceiver, group);
    }

    [Access(typeof(PPGNodeGroup))]
    public void UpdateCirculatorConnectivity(
        EntityUid uid,
        PPGNodeGroup group,
        PPGCirculatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
    }

    private void GeneratorPowerChange(EntityUid uid, PPGeneratorComponent component, ref PowerChangedEvent args)
    {
        // TODO: I wish power events didn't go out on shutdown.
        if (TerminatingOrDeleted(uid))
            return;
        var nodeGroup = GetNodeGroup(uid);
        if (nodeGroup == null)
            return;
        UpdateAppearance(uid, component, Comp<ApcPowerReceiverComponent>(uid), nodeGroup);
    }

    /// <returns>Null if the node group is not yet available. This can happen during initialization.</returns>
    private PPGNodeGroup? GetNodeGroup(EntityUid uidGenerator)
    {
        NodeContainerComponent? nodeContainer = null;
        if (!_nodeContainerQuery.Resolve(uidGenerator, ref nodeContainer))
            return null;

        if (!nodeContainer.Nodes.TryGetValue(NodeNamePPG, out var ppgNode))
            return null;

        if (ppgNode.NodeGroup is not PPGNodeGroup ppgGroup)
            return null;

        return ppgGroup;
    }

    private static (GasMixture, float δp) GetCirculatorAirTransfer(GasMixture airInlet, GasMixture airOutlet)
    {
        var n1 = airInlet.TotalMoles;
        var n2 = airOutlet.TotalMoles;
        var p1 = airInlet.Pressure;
        var p2 = airOutlet.Pressure;
        var v1 = airInlet.Volume;
        var v2 = airOutlet.Volume;
        var t1 = airInlet.Temperature;
        var t2 = airOutlet.Temperature;

        var deltap = p1 - p2;

        var denom = t1 * v2 + t2 * v1;

        if (deltap > 0 && p1 > 0 && denom > 0)
        {
            var transferMoles = n1 - (n1 + n2) * t2 * v1 / denom;
            return (airInlet.Remove(transferMoles), deltap);
        }

        return (new GasMixture(), deltap);
    }

    private (PipeNode inlet, PipeNode outlet) GetPipes(EntityUid uidCirculator)
    {
        var nodeContainer = _nodeContainerQuery.GetComponent(uidCirculator);
        var inlet = (PipeNode)nodeContainer.Nodes[NodeNameInlet];
        var outlet = (PipeNode)nodeContainer.Nodes[NodeNameOutlet];

        return (inlet, outlet);
    }
}
