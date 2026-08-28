// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Dismissinator;

/// <summary>
///     Gates firing of the "увольнятор" on it being loaded with an authorized ID card, paper and a stamp.
///     Shared so that the client predicts the refusal instead of eating a rubberband.
/// </summary>
public sealed class SharedDismissinatorSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DismissinatorComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<DismissinatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DismissinatorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DismissinatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DismissinatorComponent, GotEmaggedEvent>(OnEmagged);
    }

    /// <summary>
    ///     Unlocks the third mode. Nothing else about the gun changes.
    /// </summary>
    private void OnEmagged(Entity<DismissinatorComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnUseInHand(Entity<DismissinatorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ToggleMode(ent, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(Entity<DismissinatorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("dismissinator-verb-toggle-mode"),
            Act = () => ToggleMode(ent, user),
        });
    }

    private void OnExamined(Entity<DismissinatorComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("dismissinator-examine-mode", ("mode", GetModeName(ent.Comp.Mode))));
    }

    public void ToggleMode(Entity<DismissinatorComponent> ent, EntityUid user)
    {
        ent.Comp.Mode = ent.Comp.Mode switch
        {
            DismissinatorMode.Dismissal => DismissinatorMode.Expansion,
            DismissinatorMode.Expansion when _emag.CheckFlag(ent, EmagType.Interaction) => DismissinatorMode.Objective,
            _ => DismissinatorMode.Dismissal,
        };

        Dirty(ent);

        _popup.PopupClient(Loc.GetString("dismissinator-mode-switched", ("mode", GetModeName(ent.Comp.Mode))), ent, user);
    }

    public string GetModeName(DismissinatorMode mode)
    {
        return Loc.GetString(mode switch
        {
            DismissinatorMode.Expansion => "dismissinator-mode-expansion",
            DismissinatorMode.Objective => "dismissinator-mode-objective",
            _ => "dismissinator-mode-dismissal",
        });
    }

    private void OnAttemptShoot(Entity<DismissinatorComponent> ent, ref AttemptShootEvent args)
    {
        string? reason = null;

        if (GetIdCard(ent) is not { } idCard)
            reason = "dismissinator-no-id";
        else if (!IsAuthorized(ent, idCard))
            reason = "dismissinator-no-authority";
        else if (GetPaper(ent) == null)
            reason = "dismissinator-no-paper";
        else if (GetStamp(ent) == null)
            reason = "dismissinator-no-stamp";

        if (reason == null)
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString(reason);
    }

    /// <summary>
    ///     The ID card inserted into the authorization slot, if any.
    /// </summary>
    public Entity<IdCardComponent>? GetIdCard(Entity<DismissinatorComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, ent.Comp.IdSlotId) is not { } item
            || !TryComp<IdCardComponent>(item, out var idCard))
        {
            return null;
        }

        return (item, idCard);
    }

    public Entity<PaperComponent>? GetPaper(Entity<DismissinatorComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, ent.Comp.PaperSlotId) is not { } item
            || !TryComp<PaperComponent>(item, out var paper))
        {
            return null;
        }

        return (item, paper);
    }

    public Entity<StampComponent>? GetStamp(Entity<DismissinatorComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, ent.Comp.StampSlotId) is not { } item
            || !TryComp<StampComponent>(item, out var stamp))
        {
            return null;
        }

        return (item, stamp);
    }

    /// <summary>
    ///     True if the inserted card carries the access level that lets it hand out and revoke access.
    /// </summary>
    public bool IsAuthorized(Entity<DismissinatorComponent> ent, EntityUid idCard)
    {
        return GetAccessTags(idCard).Contains(ent.Comp.RequiredAccess);
    }

    public List<ProtoId<AccessLevelPrototype>> GetAccessTags(EntityUid idCard)
    {
        return _accessReader.FindAccessTags(idCard).ToList();
    }

    /// <summary>
    ///     Everything loaded and authorized, ready to fire.
    /// </summary>
    public bool TryGetLoadout(Entity<DismissinatorComponent> ent,
        [NotNullWhen(true)] out Entity<IdCardComponent>? idCard,
        [NotNullWhen(true)] out Entity<PaperComponent>? paper,
        [NotNullWhen(true)] out Entity<StampComponent>? stamp)
    {
        paper = null;
        stamp = null;

        idCard = GetIdCard(ent);
        if (idCard == null || !IsAuthorized(ent, idCard.Value))
        {
            idCard = null;
            return false;
        }

        paper = GetPaper(ent);
        stamp = GetStamp(ent);

        if (paper != null && stamp != null)
            return true;

        idCard = null;
        paper = null;
        stamp = null;
        return false;
    }
}
