using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using Content.Shared.PrinterDoc; // Imperial PrinterDoc
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Lathe.UI
{
    [UsedImplicitly]
    public sealed class LatheBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private LatheMenu? _menu;
        public LatheBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindowCenteredRight<LatheMenu>();
            _menu.SetEntity(Owner);

            _menu.OnServerListButtonPressed += _ =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };

            _menu.RecipeQueueAction += (recipe, amount) =>
            {
                SendMessage(new LatheQueueRecipeMessage(recipe, amount));
            };

            // Imperial PrinterDoc Start
            _menu.OnUseCardIdCheckBoxChanged += useCardId =>
            {
                SendMessage(new PrinterDocCheckIdCardMessage(useCardId));
            };
            // Imperial PrinterDoc End

            _menu.QueueDeleteAction += index => SendMessage(new LatheDeleteRequestMessage(index));
            _menu.QueueMoveUpAction += index => SendMessage(new LatheMoveRequestMessage(index, -1));
            _menu.QueueMoveDownAction += index => SendMessage(new LatheMoveRequestMessage(index, 1));
            _menu.DeleteFabricatingAction += () => SendMessage(new LatheAbortFabricationMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            // Imperial PrinterDoc
            if (_menu == null)
                return;
            if (state is LatheUpdateState msg)
            {
                _menu.Recipes = msg.Recipes;
                _menu.PopulateRecipes();
                _menu.UpdateCategories();
                _menu.PopulateQueueList(msg.Queue);
                _menu.SetQueueInfo(msg.CurrentlyProducing);
                _menu.SetUseCardId(msg.UseCardId);
            }
        }
    }
}
