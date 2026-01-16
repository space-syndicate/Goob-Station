using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Botany.Components;
using Content.Server.Imperial.PlantsAnalyzer.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Imperial.PlantsAnalyzer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.PowerCell;

namespace Content.Server.Imperial.PlantsAnalyzer;

public sealed class PlantsAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantsAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantsAnalyzerComponent, PlantsAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantsAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<PlantsAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<PlantsAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<PlantsAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not { } patient)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            var patientCoordinates = Transform(patient).Coordinates;
            if (!_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange))
            {
                StopAnalyzingEntity((uid, component), patient);
                continue;
            }

            UpdateScannedUser(uid, patient, true);
        }
    }

    private void OnAfterInteract(Entity<PlantsAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantHolderComponent>(args.Target) || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new PlantsAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;
    }

    private void OnDoAfter(Entity<PlantsAnalyzerComponent> uid, ref PlantsAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    private void OnInsertedIntoContainer(Entity<PlantsAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OnToggled(Entity<PlantsAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
            StopAnalyzingEntity(ent, patient);
    }

    private void OnDropped(Entity<PlantsAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, PlantsAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, PlantsAnalyzerUiKey.Key, user);
    }

    private void BeginAnalyzingEntity(Entity<PlantsAnalyzerComponent> plantsAnalyzer, EntityUid target)
    {
        plantsAnalyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(plantsAnalyzer.Owner);

        UpdateScannedUser(plantsAnalyzer, target, true);
    }

    private void StopAnalyzingEntity(Entity<PlantsAnalyzerComponent> plantsAnalyzer, EntityUid target)
    {
        plantsAnalyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(plantsAnalyzer.Owner);

        UpdateScannedUser(plantsAnalyzer, target, false);
    }

    public void UpdateScannedUser(EntityUid plantsAnalyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(plantsAnalyzer, PlantsAnalyzerUiKey.Key))
            return;

        if (!TryComp<PlantHolderComponent>(target, out var plantHolder))
            return;

        var plantName = Loc.GetString(plantHolder.Seed?.DisplayName ?? "plants-analyzer-window-unknown-plant");

        var potencyLevel = plantHolder.Seed?.Potency ?? 0f;

        var productionLevel = plantHolder.Seed?.Production ?? 0f;

        var isDead = plantHolder.Dead;

        var optimalConditions = new List<string>();

        if (plantHolder.ImproperHeat)
        {
            var idealHeat = plantHolder.Seed?.IdealHeat ?? 0f;
            var heatTolerance = plantHolder.Seed?.HeatTolerance ?? 0f;

            optimalConditions.Add($"{Loc.GetString("plants-analyzer-optimal-temperature")} {idealHeat:F1} ± {heatTolerance:F1} " + Loc.GetString("units-kelvin"));
        }

        if (plantHolder.ImproperPressure)
        {
            var lowPressure = plantHolder.Seed?.LowPressureTolerance ?? 0f;
            var highPressure = plantHolder.Seed?.HighPressureTolerance ?? 0f;

            optimalConditions.Add($"{Loc.GetString("plants-analyzer-optimal-pressure")} {lowPressure:F1} - {highPressure:F1} " + Loc.GetString("units-k-pascal"));
        }

        if (plantHolder.ImproperLight)
        {
            var idealLight = plantHolder.Seed?.IdealLight ?? 0f;

            optimalConditions.Add($"{Loc.GetString("plants-analyzer-optimal-light")} {idealLight:F1}");
        }

        var conditionsMessage = string.Join("\n", optimalConditions);

        var hasKudzu = plantHolder.Seed?.TurnIntoKudzu ?? false;

        var mutations = plantHolder.Seed?.Mutations.Any() == true
            ? string.Join(",\n      " + string.Concat(Enumerable.Repeat(" ", Loc.GetString("plants-analyzer-window-mutations").Length * 2)),
                plantHolder.Seed.Mutations.Select(mutation =>
                    Loc.GetString("random-mutation-name-" +
                    Regex.Replace(mutation.Name, @"(?<=[a-z])([A-Z])", "-$1").ToLower())))
            : Loc.GetString("plants-analyzer-window-no-mutations");

        var chemicals = plantHolder.Seed?.Chemicals.Any() == true
            ? string.Join(",\n      " + string.Concat(Enumerable.Repeat(" ", Loc.GetString("plants-analyzer-window-chemicals").Length * 2)),
                plantHolder.Seed.Chemicals.Keys.Select(reagent =>
                    Loc.GetString("reagent-name-" +
                    Regex.Replace(reagent, @"(?<=[a-z])([A-Z])", "-$1").ToLower())))
            : Loc.GetString("plants-analyzer-window-no-chemicals");

        _uiSystem.ServerSendUiMessage(plantsAnalyzer, PlantsAnalyzerUiKey.Key, new PlantsAnalyzerScannedUserMessage(
            GetNetEntity(target),
            plantName,
            potencyLevel,
            productionLevel,
            plantHolder.PestLevel,
            plantHolder.WeedLevel,
            plantHolder.Toxins,
            plantHolder.Age,
            plantHolder.Health,
            plantHolder.MutationLevel,
            plantHolder.ImproperHeat,
            plantHolder.ImproperPressure,
            plantHolder.ImproperLight,
            isDead,
            scanMode,
            conditionsMessage,
            hasKudzu,
            mutations,
            chemicals
        ));
    }

}
