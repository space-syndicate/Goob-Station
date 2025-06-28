using System.Numerics;
using Content.Server.Decals;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Imperial.DecalGun;
using Content.Shared.Interaction;

namespace Content.Server.Imperial.DecalGun;

/// <summary>
/// Server-side system for managing the functionality of decal guns, including decal placement,
/// state synchronization, charge consumption, UI updates, and examination feedback.
/// Inherits shared slot-handling logic from <see cref="SharedDecalGunSystem"/>.
/// </summary>
public sealed class DecalGunSystem : SharedDecalGunSystem
{
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DecalGunComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DecalGunComponent, DecalGunSelectionChangedMessage>(OnSelectionChanged);
        SubscribeLocalEvent<DecalGunComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<DecalGunComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<DecalGunMagComponent, ComponentInit>(OnInit);
    }

    /// <summary>
    /// Handles decal placement when the user interacts with the world.
    /// Applies snap-to-tile logic, checks range and charge availability,
    /// and spawns a decal at the target location.
    /// </summary>
    private void OnAfterInteract(Entity<DecalGunComponent> ent, ref AfterInteractEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.ChosenDecal))
            return;

        if (args.Handled || !args.CanReach)
            return;

        var coords = args.ClickLocation;

        if (!coords.IsValid(EntityManager))
            return;

        if (!TryUseCharge(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("decal-gun-no-ammo-left"), args.User, args.User);
            return;
        }

        if (ent.Comp.IsSnap)
        {
            var newPos = new Vector2(
                (float) (MathF.Round(coords.X - 0.5f, MidpointRounding.AwayFromZero) + 0.5),
                (float) (MathF.Round(coords.Y - 0.5f, MidpointRounding.AwayFromZero) + 0.5)
            );
            coords = coords.WithPosition(newPos);
        }

        coords = coords.Offset(new Vector2(-0.5f, -0.5f));

        _decals.TryAddDecal(ent.Comp.ChosenDecal,
            coords,
            out _,
            ent.Comp.ChosenColor,
            Angle.FromDegrees(ent.Comp.Rotation),
            cleanable: ent.Comp.IsCleanable);
    }

    /// <summary>
    /// Updates decal gun component state based on selections from the user interface.
    /// </summary>
    private void OnSelectionChanged(Entity<DecalGunComponent> ent, ref DecalGunSelectionChangedMessage args)
    {
        ent.Comp.ChosenDecal = args.DecalId;
        ent.Comp.ChosenColor = args.Color;
        ent.Comp.IsSnap = args.SnapToTile;
        ent.Comp.Rotation = args.Rotation;
        ent.Comp.IsCleanable = args.IsCleanable;
    }

    /// <summary>
    /// Sends initial component state to the UI upon opening to preserve decal selection and settings.
    /// </summary>
    private void OnUIOpened(Entity<DecalGunComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.ChosenDecal))
            return;

        var uiMessage = new DecalGunUIState(
            ent.Comp.ChosenDecal,
            ent.Comp.ChosenColor,
            ent.Comp.IsSnap,
            ent.Comp.Rotation,
            ent.Comp.IsCleanable);

        _ui.SetUiState(ent.Owner, args.UiKey, uiMessage);
    }

    private void OnExamined(Entity<DecalGunComponent> ent, ref ExaminedEvent args)
    {
        var magComp = GetCurrentMag(ent.Owner);

        if (magComp == null)
        {
            args.PushText(Loc.GetString("decal-gun-no-cartridge"));
            return;
        }

        args.PushText(Loc.GetString("decal-gun-examined", ("charges", magComp.Value.Comp.CurrentCharges)));
    }

    /// <summary>
    /// Initializes magazine state to max charges on component startup.
    /// </summary>
    private void OnInit(Entity<DecalGunMagComponent> ent, ref ComponentInit args)
    {
        ent.Comp.CurrentCharges = ent.Comp.MaxCharges;
        Dirty(ent);
    }

    /// <summary>
    /// Attempts to use a charge from the inserted magazine, returning true on success.
    /// </summary>
    private bool TryUseCharge(EntityUid weapon)
    {
        var magComp = GetCurrentMag(weapon);
        return magComp != null && magComp.Value.Comp.TryUseCharge();
    }
}
