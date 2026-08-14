// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Goobstation.Shared.Disease.Components;
using Content.Server.Body.Systems;
using Content.Server.Medical.Components;
using Content.Shared._CorvaxGoob.Medical.HealthAnalyzer;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.Medical;

/// <summary>
/// Prints a receipt-style snapshot of the patient currently scanned by a health analyzer.
/// </summary>
public sealed class HealthAnalyzerPrintSystem : EntitySystem
{
    private static readonly SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly WoundSystem _woundSystem = default!;
    [Dependency] private readonly TraumaSystem _traumaSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<HealthAnalyzerComponent>(HealthAnalyzerUiKey.Key, subs =>
        {
            subs.Event<HealthAnalyzerPrintMessage>(OnPrint);
        });
    }

    private void OnPrint(Entity<HealthAnalyzerComponent> analyzer, ref HealthAnalyzerPrintMessage args)
    {
        if (analyzer.Comp.ScannedEntity is not { } target || Deleted(target))
        {
            _popupSystem.PopupEntity("Нет данных пациента для печати.", analyzer.Owner, args.Actor);
            return;
        }

        if (!TryComp<BodyComponent>(target, out var body))
        {
            _popupSystem.PopupEntity("Не удалось получить медицинские данные пациента.", analyzer.Owner, args.Actor);
            return;
        }

        var printed = Spawn("MedicalReportPaper", Transform(analyzer.Owner).Coordinates);
        _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paper))
        {
            Log.Error("Health analyzer printed an entity without PaperComponent.");
            return;
        }

        var patientName = MetaData(target).EntityName;
        _metaData.SetEntityName(printed, $"Медицинский отчёт — {patientName}");
        _paperSystem.SetContent((printed, paper), BuildReport(target, body));

        _audioSystem.PlayPvs(PrintSound, analyzer.Owner,
            AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));
    }

    private string BuildReport(EntityUid target, BodyComponent body)
    {
        var text = new StringBuilder();
        var patientName = MetaData(target).EntityName;

        text.AppendLine("МЕДИЦИНСКИЙ ОТЧЁТ");
        text.AppendLine();
        text.AppendLine($"Пациент: {patientName}");
        text.AppendLine(GetPatientTypeName(target));
        text.AppendLine($"Время сканирования: {_timing.CurTime:hh\\:mm\\:ss}");

        var status = Loc.GetString("health-analyzer-window-entity-unknown-text");
        if (TryComp<MobStateComponent>(target, out var mobState))
        {
            status = mobState.CurrentState switch
            {
                MobState.Alive => Colorize(Loc.GetString("health-analyzer-window-entity-alive-text"), "#2E7D32"),
                MobState.Critical => Colorize(Loc.GetString("health-analyzer-window-entity-critical-text"), "#B8860B"),
                MobState.Dead => Colorize(Loc.GetString("health-analyzer-window-entity-dead-text"), "#C62828"),
                _ => status,
            };
        }

        text.AppendLine($"Статус: {status}");

        if (TryComp<TemperatureComponent>(target, out var temperature))
            text.AppendLine($"Температура: {temperature.CurrentTemperature - 273.15f:F1} °C ({temperature.CurrentTemperature:F1} K)");

        var bloodLevel = _bloodstreamSystem.GetBloodLevel(target);
        text.AppendLine($"Уровень крови: {bloodLevel * 100f:F1} %");

        AppendDamage(text, target);
        AppendBodyCondition(text, target, body, bloodLevel);
        AppendDiseases(text, target);
        AppendOrgans(text, target);
        AppendChemicals(text, target, body);

        return text.ToString();
    }

    private string GetPatientTypeName(EntityUid target)
    {
        if (TryComp<HumanoidAppearanceComponent>(target, out var humanoid) &&
            _prototypes.TryIndex<SpeciesPrototype>(humanoid.Species, out var species))
        {
            return Loc.GetString(species.Name);
        }

        var prototypeName = MetaData(target).EntityPrototype?.Name;
        return string.IsNullOrWhiteSpace(prototypeName)
            ? Loc.GetString("health-analyzer-window-entity-unknown-species-text")
            : prototypeName;
    }

    private void AppendDamage(StringBuilder text, EntityUid target)
    {
        text.AppendLine();
        text.AppendLine("ПОВРЕЖДЕНИЯ:");

        if (!TryComp<DamageableComponent>(target, out var damageable))
        {
            text.AppendLine("Повреждений: 0");
            return;
        }

        var hasDamage = damageable.Damage.DamageDict.Any(entry => entry.Value > 0);
        if (!hasDamage)
        {
            text.AppendLine("Повреждений: 0");
            return;
        }

        text.AppendLine($"Общие повреждения: {damageable.TotalDamage}");
        text.AppendLine($"Суммарный урон: {_threshold.CheckVitalDamage(target, damageable)}");

        foreach (var (damageTypeId, amount) in damageable.Damage.DamageDict.OrderByDescending(x => x.Value))
        {
            if (amount <= 0)
                continue;

            var name = damageTypeId;
            if (_prototypes.TryIndex<DamageTypePrototype>(damageTypeId, out var prototype))
                name = prototype.LocalizedName;

            var damageLine = Loc.GetString(
                "health-analyzer-window-damage-type-text",
                ("damageType", name),
                ("amount", amount));

            text.AppendLine(Colorize($"- {damageLine}", GetDamageColor(damageTypeId)));
        }
    }

    private void AppendBodyCondition(StringBuilder text, EntityUid target, BodyComponent body, float bloodLevel)
    {
        text.AppendLine();
        text.AppendLine("СОСТОЯНИЕ:");

        var any = false;
        var patientName = MetaData(target).EntityName;

        if (TryComp<BloodstreamComponent>(target, out var bloodstream) && bloodLevel < bloodstream.BloodlossThreshold)
        {
            any = true;
            text.AppendLine(Colorize(
                Loc.GetString("condition-body-low-blood", ("entity", patientName)),
                "#C62828"));
        }

        if (TryComp<UnrevivableComponent>(target, out var unrevivable) && unrevivable.Analyzable)
        {
            any = true;
            text.AppendLine(Loc.GetString("condition-body-unrevivable", ("entity", patientName)));
        }

        if (body.RootContainer.ContainedEntity is { } rootPart)
        {
            var woundables = _woundSystem.GetAllWoundableChildren(rootPart).ToList();

            foreach (var (woundable, woundableComp) in woundables)
            {
                if (woundableComp.Bleeds <= 0)
                    continue;

                any = true;
                var bodyPart = _bodySystem.GetTargetBodyPart(woundable);
                text.AppendLine(Loc.GetString(
                    $"condition-body-bleeding-{bodyPart}",
                    ("entity", patientName)));
            }

            foreach (var (woundable, _) in woundables)
            {
                if (!_traumaSystem.TryGetWoundableTrauma(woundable, out var traumas))
                    continue;

                foreach (var trauma in traumas)
                {
                    var traumaType = trauma.Comp.TraumaType.ToString();

                    // Dismemberment is represented below by the actual missing body part.
                    // Organ damage is represented in the organs section by its integrity percentage.
                    if (traumaType is "Dismemberment" or "OrganDamage")
                        continue;

                    any = true;
                    var woundableName = MetaData(woundable).EntityName;

                    if (trauma.Comp.TraumaType == TraumaSystem.BoneDamage &&
                        trauma.Comp.TraumaTarget is { } bone &&
                        TryComp<BoneComponent>(bone, out var boneComp))
                    {
                        text.AppendLine(Loc.GetString(
                            $"condition-body-trauma-BoneDamage-{boneComp.BoneSeverity}",
                            ("woundable", woundableName)));
                        continue;
                    }

                    text.AppendLine(Loc.GetString(
                        $"condition-body-trauma-{traumaType}",
                        ("woundable", woundableName)));
                }
            }
        }

        if (HasComp<TargetingComponent>(target))
        {
            var bodyState = _woundSystem.GetDamageableStatesOnBody(target);
            var severedParts = bodyState
                .Where(entry => entry.Value == WoundableSeverity.Severed)
                .Select(entry => entry.Key)
                .ToHashSet();

            foreach (var part in severedParts)
            {
                if (part is TargetBodyPart.Chest or TargetBodyPart.Groin)
                    continue;

                if (part == TargetBodyPart.LeftHand && severedParts.Contains(TargetBodyPart.LeftArm) ||
                    part == TargetBodyPart.RightHand && severedParts.Contains(TargetBodyPart.RightArm) ||
                    part == TargetBodyPart.LeftFoot && severedParts.Contains(TargetBodyPart.LeftLeg) ||
                    part == TargetBodyPart.RightFoot && severedParts.Contains(TargetBodyPart.RightLeg))
                {
                    continue;
                }

                any = true;
                text.AppendLine($"• {GetRussianBodyPartName(part)}: отсутствует");
            }
        }

        if (!any)
            text.AppendLine(Loc.GetString("condition-none"));
    }

    private void AppendDiseases(StringBuilder text, EntityUid target)
    {
        if (!TryComp<DiseaseCarrierComponent>(target, out var carrier) || carrier.Diseases.ContainedEntities.Count == 0)
            return;

        var first = true;
        foreach (var diseaseUid in carrier.Diseases.ContainedEntities)
        {
            if (!TryComp<DiseaseComponent>(diseaseUid, out var disease))
                continue;

            text.AppendLine();
            if (!first)
                text.AppendLine("---");

            first = false;
            text.AppendLine(Loc.GetString(
                "health-analyzer-window-disease-type-text",
                ("type", disease.Genotype)));
            text.AppendLine(Loc.GetString(
                "health-analyzer-window-disease-progress-text",
                ("progress", disease.InfectionProgress)));
            text.AppendLine(Loc.GetString(
                "health-analyzer-window-immunity-progress-text",
                ("progress", disease.ImmunityProgress)));
        }
    }

    private void AppendOrgans(StringBuilder text, EntityUid target)
    {
        text.AppendLine();
        text.AppendLine("ОРГАНЫ:");

        var any = false;
        foreach (var (organUid, organComp) in _bodySystem.GetBodyOrgans(target))
        {
            if (organComp.IntegrityCap <= 0)
                continue;

            any = true;
            var organName = MetaData(organUid).EntityName;
            var percent = organComp.OrganIntegrity / organComp.IntegrityCap * 100;

            text.AppendLine($"- {Loc.GetString(
                "group-organ-status",
                ("organ", organName),
                ("capacity", percent))}");

            if (HasComp<RottingComponent>(organUid))
                text.AppendLine(Loc.GetString("condition-organ-rotting", ("organ", organName)));
        }

        if (!any)
            text.AppendLine("Не обнаружено.");
    }

    private void AppendChemicals(StringBuilder text, EntityUid target, BodyComponent body)
    {
        text.AppendLine();
        text.AppendLine("ХИМИКАТЫ:");

        var any = false;

        if (TryComp(target, out SolutionContainerManagerComponent? container))
        {
            foreach (var (name, solution) in _solutionContainerSystem.EnumerateSolutions((target, container)))
            {
                if (name is null ||
                    name == BloodstreamComponent.DefaultBloodTemporarySolutionName ||
                    name == "print")
                    continue;

                if (AppendSolution(text, solution.Comp.Solution))
                    any = true;
            }
        }

        if (_bodySystem.TryGetBodyOrganEntityComps<StomachComponent>((target, body), out var stomachs))
        {
            foreach (var stomach in stomachs)
            {
                if (stomach.Comp1.Solution is null)
                    continue;

                if (AppendSolution(text, stomach.Comp1.Solution.Value.Comp.Solution))
                    any = true;
            }
        }

        if (!any)
            text.AppendLine("Не обнаружено.");
    }

    private bool AppendSolution(StringBuilder text, Solution solution)
    {
        var reagents = solution.Contents.Where(x => x.Quantity > 0).ToList();
        if (reagents.Count == 0)
            return false;

        var solutionName = solution.Name is { } name
            ? Loc.GetString("group-solution-name", ("solution", Loc.GetString($"solution-type-{name}")))
            : Loc.GetString("group-solution-unknown");

        text.AppendLine($"{solutionName}:");
        foreach (var reagent in reagents)
        {
            var reagentName = reagent.Reagent.Prototype;
            if (_prototypes.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var prototype))
                reagentName = prototype.LocalizedName;

            text.AppendLine($"- {Loc.GetString(
                "group-solution-contents",
                ("reagent", reagentName),
                ("quantity", reagent.Quantity))}");
        }

        return true;
    }

    private static string Colorize(string text, string? color)
    {
        return color is null ? text : $"[color={color}]{text}[/color]";
    }

    private static string? GetDamageColor(string damageTypeId)
    {
        return damageTypeId.ToLowerInvariant() switch
        {
            "blunt" => "#A93226",
            "slash" => "#C0392B",
            "piercing" => "#922B21",
            "heat" => "#D35400",
            "cold" => "#2874A6",
            "shock" => "#B7950B",
            "caustic" => "#6C8E23",
            "asphyxiation" => "#5D6D7E",
            "bloodloss" => "#B03A2E",
            "poison" => "#1E8449",
            "radiation" => "#7D3C98",
            "cellular" => "#884EA0",
            "structural" => "#616A6B",
            _ => null,
        };
    }

    private static string GetRussianBodyPartName(TargetBodyPart part)
    {
        return part switch
        {
            TargetBodyPart.Head => "Голова",
            TargetBodyPart.Chest => "Грудь",
            TargetBodyPart.Groin => "Пах",
            TargetBodyPart.LeftArm => "Левая рука",
            TargetBodyPart.LeftHand => "Левая кисть",
            TargetBodyPart.RightArm => "Правая рука",
            TargetBodyPart.RightHand => "Правая кисть",
            TargetBodyPart.LeftLeg => "Левая нога",
            TargetBodyPart.LeftFoot => "Левая стопа",
            TargetBodyPart.RightLeg => "Правая нога",
            TargetBodyPart.RightFoot => "Правая стопа",
            _ => part.ToString(),
        };
    }
}
