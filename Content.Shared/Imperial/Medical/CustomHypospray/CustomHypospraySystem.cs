using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Body.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.FixedPoint;

namespace Content.Shared.Imperial.Medical
{
    public sealed class CustomHypospraySystem : EntitySystem
    {
        [Dependency] private readonly HypospraySystem _hypospray = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CustomHyposprayComponent, AfterInteractEvent>(OnAfterInteract, before: new[] { typeof(HypospraySystem) });
            SubscribeLocalEvent<CustomHyposprayComponent, DoAfterCustomHyposprayEvent>(OnDoAfterComplete);
            SubscribeLocalEvent<CustomHyposprayComponent, UseInHandEvent>(OnUseInHand, before: new[] { typeof(HypospraySystem) });
        }

        private void OnAfterInteract(EntityUid uid, CustomHyposprayComponent comp, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null || args.Handled)
                return;

            if (Terminating(uid))
                return;

            if (!HasComp<BodyComponent>(args.Target.Value) || !TryComp<HyposprayComponent>(uid, out var hyposprayComp))
                return;

            if (!_solutionContainer.TryGetSolution(uid, hyposprayComp.SolutionName, out var soln, out var solution) || solution.Volume == 0)
                return;

            var doAfterArgs = new DoAfterArgs(
                EntityManager,
                args.User,
                comp.InjectionDelayTime,
                new DoAfterCustomHyposprayEvent(),
                uid,
                args.Target.Value,
                uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                DistanceThreshold = 1.5f
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
            _popup.PopupClient(Loc.GetString("hypospray-component-inject-start-message"), args.User, args.User);
            _popup.PopupClient(Loc.GetString("hypospray-component-inject-person-message"), args.Target.Value, args.Target.Value, PopupType.Large);
            args.Handled = true;
        }

        private void OnDoAfterComplete(EntityUid uid, CustomHyposprayComponent comp, DoAfterCustomHyposprayEvent args)
        {
            if (args.Cancelled || args.Handled)
            {
                if (args.Cancelled)
                    _popup.PopupClient(Loc.GetString("hypospray-component-inject-cancelled-message"), args.User, args.User);
                return;
            }

            if (args.Args.Target == null)
                return;

            if (!HasComp<BodyComponent>(args.Args.Target.Value) || !TryComp<HyposprayComponent>(uid, out var hyposprayComp))
                return;

            if (_solutionContainer.TryGetSolution(uid, hyposprayComp.SolutionName, out var soln, out var solution))
            {
                if (IsHarmfulSolution(solution, hyposprayComp.TransferAmount))
                {
                    _popup.PopupClient(Loc.GetString("hypospray-component-inject-toxin-message"), args.User, args.User, PopupType.LargeCaution);
                    args.Handled = true;
                    return;
                }
            }

            args.Handled = true;
            _hypospray.TryDoInject((uid, hyposprayComp), args.Args.Target.Value, args.Args.User);
        }

        /// <summary>
        /// проверяет, является ли раствор вредным, сравнивая общий урон и лечение
        /// </summary>
        private bool IsHarmfulSolution(Solution solution, FixedPoint2 transferAmount)
        {
            float totalHealing = 0f;
            float totalDamage = 0f;

            foreach (var reagent in solution.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var proto))
                    continue;

                if (proto.Metabolisms == null)
                    continue;

                // рассчитываем количество реагента в дозе
                float reagentInDose = (float)(reagent.Quantity / solution.Volume * transferAmount);

                foreach (var (metabolismId, effects) in proto.Metabolisms)
                {
                    var metabolismIdStr = metabolismId.ToString();

                    foreach (var effect in effects.Effects)
                    {
                        var effectType = effect.GetType().Name;

                        // подсчитываем урон
                        if (metabolismIdStr.Contains("Toxin") ||
                            metabolismIdStr.Contains("Poison") ||
                            metabolismIdStr.Contains("Narcotic"))
                        {
                            totalDamage += reagentInDose;
                            continue;
                        }

                        // подсчитываем лечение
                        if (effectType.Contains("Health") ||
                            effectType.Contains("Heal") ||
                            metabolismIdStr.Contains("Medicine"))
                        {
                            totalHealing += reagentInDose;
                        }
                    }
                }
            }

            // добавляем 1 ед к итоговому лечению, чтобы незначительный урон не блокировал лекарство
            return totalDamage > totalHealing + 1;
        }

        private void OnUseInHand(EntityUid uid, CustomHyposprayComponent comp, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HyposprayComponent>(uid, out var hyposprayComp))
                return;

            if (!_solutionContainer.TryGetSolution(uid, hyposprayComp.SolutionName, out var soln, out var solution) || solution.Volume == 0)
                return;

            if (!HasComp<BodyComponent>(args.User))
                return;

            var doAfterArgs = new DoAfterArgs(
                EntityManager,
                args.User,
                comp.InjectionDelayTime,
                new DoAfterCustomHyposprayEvent(),
                uid,
                args.User,
                uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
            _popup.PopupClient(Loc.GetString("hypospray-component-inject-start-message"), args.User, args.User);
            args.Handled = true;
        }
    }
}
