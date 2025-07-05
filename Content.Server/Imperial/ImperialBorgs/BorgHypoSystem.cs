using Content.Server.Chemistry.Components;
using Content.Shared.Actions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Shared.Imperial.ImperialBorgs;
using Content.Shared.Imperial.ImperialBorgs.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
namespace Content.Server.Imperial.ImperialBorgs
{
    public sealed class BorgHypoSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = null!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = null!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BorgHypoComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchVerb);
            SubscribeLocalEvent<BorgHypoComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<BorgHypoComponent, ChangeReagentAction>(OnReagentAction);
            SubscribeNetworkEvent<ChangeReagentEvent>(OnReagentChange);
            SubscribeLocalEvent<BorgHypoComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnGetActions(EntityUid uid, BorgHypoComponent component, GetItemActionsEvent args)
        {
            args.AddAction(ref component.ActionEntity, component.Action);
        }

        private void OnReagentAction(EntityUid uid, BorgHypoComponent component, ChangeReagentAction args)
        {
            if (args.Handled)
                return;

            RaiseNetworkEvent(new OpenBorgHypoUIEvent(GetNetEntity(uid)), args.Performer);
            args.Handled = true;
        }

        private void OnReagentChange(ChangeReagentEvent msg)
        {
            var uid = GetEntity(msg.Entity);
            if (!TryComp<BorgHypoComponent>(uid, out var component))
            {
                return;
            }


            if (msg.ReagentId == null)
                return;

            if (_prototypeManager.TryIndex(msg.ReagentId, out ReagentPrototype? reagent))
            {
                SwitchReagent(uid, component, reagent);
            }
        }

        private void AddSwitchVerb(EntityUid uid, BorgHypoComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.Solutions.Count <= 1)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    RaiseNetworkEvent(new OpenBorgHypoUIEvent(GetNetEntity(uid)), args.User);
                },
                Text = Loc.GetString("borghypo-switchreagent"),
                Priority = 1
            };
            args.Verbs.Add(verb);
        }
        private void OnUseInHand(Entity<BorgHypoComponent> entity, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            RaiseNetworkEvent(new OpenBorgHypoUIEvent(GetNetEntity(entity)), args.User);
            args.Handled = true;
        }

        private void SwitchReagent(EntityUid uid, BorgHypoComponent component, ReagentPrototype? reagent = null)
        {

            if (!TryComp<SolutionRegenerationComponent>(uid, out var solutionRegenerationComponent))
            {
                return;
            }

            if (!_solutionSystem.TryGetSolution(uid, solutionRegenerationComponent.SolutionName, out var solution))
            {
                return;
            }


            if (reagent != null)
            {
                var index = component.Solutions.FindIndex(x => x.GetPrimaryReagentId() == reagent.ID);
                if (index == -1)
                {
                    return;
                }

                component.CurrentIndex = index;
            }
            else
            {
                component.CurrentIndex = (component.CurrentIndex + 1) % component.Solutions.Count;
            }

            var newSolution = component.Solutions[component.CurrentIndex];
            var primaryId = newSolution.GetPrimaryReagentId();
            if (primaryId == null)
            {
                return;
            }

            if (!_prototypeManager.TryIndex(primaryId, out ReagentPrototype? proto))
            {
                return;
            }
            solution.Value.Comp.Solution.RemoveAllSolution();

            var generated = solutionRegenerationComponent.Generated;

            generated.RemoveAllSolution();
            foreach (var reagentQuantity in newSolution.Reagents)
            {
                generated.AddReagent(reagentQuantity.ReagentId, reagentQuantity.Quantity);
            }

            component.CurrentReagentName = proto.LocalizedName;
            component.UiUpdateNeeded = true;
            Dirty(uid, component);
        }
    }
}
