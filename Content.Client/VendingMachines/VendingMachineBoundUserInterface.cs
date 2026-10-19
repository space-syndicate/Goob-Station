// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Aquila.Economy; // Aquila Change
using Content.Client.UserInterface.Controls;
using Content.Shared._Aquila.Economy.Components; // Aquila Change
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Prototypes; // Aquila Change
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindowCenteredLeft<VendingMachineMenu>();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            _menu.OnItemSelected += OnItemSelected;
            // Aquila Change start
            if (EntMan.HasComponent<PaidVendingMachineComponent>(Owner) && PlayerManager.LocalEntity is { } player)
                _menu.GetPriceLabel = (item, type) => GetPriceLabel(player, item, type);
            // Aquila Change end
            Refresh();
        }

        public void Refresh()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory, enabled);
        }

        public void UpdateAmounts()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);
            _menu?.UpdateAmounts(_cachedInventory, enabled);
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

            if (selectedItem == null)
                return;

            SendPredictedMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        // Aquila Change start
        private string? GetPriceLabel(EntityUid player, EntProtoId item, InventoryType type)
        {
            var paid = EntMan.System<PaidVendingMachineSystem>();
            if (!paid.CanBuy(Owner, player, item, type))
                return Loc.GetString("vending-paid-restricted-label");

            var price = paid.GetPriceFor(Owner, player, item);
            return price > 0 ? Loc.GetString("vending-paid-price", ("price", price)) : null;
        }
        // Aquila Change end

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}