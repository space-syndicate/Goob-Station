using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Body.Components;
using Content.Shared.Popups;

namespace Content.Shared.Imperial.Medical
{
    public sealed class CustomHypospraySystem : EntitySystem
    {
        [Dependency] private readonly HypospraySystem _hypospray = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CustomHyposprayComponent, AfterInteractEvent>(OnAfterInteract, before: new[] { typeof(HypospraySystem) });
            SubscribeLocalEvent<CustomHyposprayComponent, DoAfterCustomHyposprayEvent>(OnDoAfterComplete);
        }

        private void OnAfterInteract(EntityUid uid, CustomHyposprayComponent comp, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null || args.Handled)
                return;

            if (!TryComp<MetaDataComponent>(uid, out var meta) || meta.EntityLifeStage >= EntityLifeStage.Terminating)
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

            args.Handled = true;
            _hypospray.TryDoInject((uid, hyposprayComp), args.Args.Target.Value, args.Args.User);
        }
    }
}
