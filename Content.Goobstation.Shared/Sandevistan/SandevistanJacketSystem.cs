// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Armor;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Sandevistan;

/// <summary>
/// While the edgerunner jacket is worn, the sandevistan toggle action's use delay is shortened.
/// </summary>
public sealed class SandevistanJacketSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private const string SandevistanActionId = "ActionToggleSandevistan";
    private const string OuterClothingSlot = "outerClothing";

    private TimeSpan _defaultUseDelay;

    public override void Initialize()
    {
        base.Initialize();

        // Read the real default use delay from the action prototype so this cannot drift from the YAML.
        _defaultUseDelay = _proto.Index<EntityPrototype>(SandevistanActionId)
            .TryGetComponent<ActionComponent>(out var actionComp, _compFactory)
            && actionComp.UseDelay is {} delay
                ? delay
                : TimeSpan.FromSeconds(3);

        SubscribeLocalEvent<SandevistanJacketComponent, GotEquippedEvent>(OnJacketEquipped);
        SubscribeLocalEvent<SandevistanJacketComponent, GotUnequippedEvent>(OnJacketUnequipped);
        SubscribeLocalEvent<SandevistanJacketComponent, ArmorExamineEvent>(OnArmorExamine);
        SubscribeLocalEvent<SandevistanUserComponent, ComponentStartup>(OnSandevistanAdded);
    }

    private void OnJacketEquipped(Entity<SandevistanJacketComponent> ent, ref GotEquippedEvent args) =>
        UpdateUseDelay(args.Equipee);

    private void OnJacketUnequipped(Entity<SandevistanJacketComponent> ent, ref GotUnequippedEvent args) =>
        UpdateUseDelay(args.Equipee);

    private void OnSandevistanAdded(Entity<SandevistanUserComponent> ent, ref ComponentStartup args)
    {
        // The action might not be attached yet by the time the organ's components are started.
        Timer.Spawn(TimeSpan.Zero, () => UpdateUseDelay(ent.Owner));
    }

    private void OnArmorExamine(Entity<SandevistanJacketComponent> ent, ref ArmorExamineEvent args)
    {
        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-sandevistan-jacket"));
    }

    private void UpdateUseDelay(EntityUid user)
    {
        var action = FindSandevistanAction(user);
        if (action == null)
            return;

        var useDelay = GetWornJacket(user) is {} jacket
            ? Comp<SandevistanJacketComponent>(jacket).UseDelay
            : _defaultUseDelay;

        _actions.SetUseDelay((action.Value, Comp<ActionComponent>(action.Value)), useDelay);
    }

    private EntityUid? FindSandevistanAction(EntityUid user)
    {
        foreach (var action in _actions.GetActions(user))
        {
            if (MetaData(action.Owner).EntityPrototype?.ID == SandevistanActionId)
                return action.Owner;
        }

        return null;
    }

    private EntityUid? GetWornJacket(EntityUid user)
    {
        if (!_inventory.TryGetSlotEntity(user, OuterClothingSlot, out var jacket))
            return null;

        return HasComp<SandevistanJacketComponent>(jacket.Value) ? jacket : null;
    }
}