using Content.Server.Ame.Components;
using Content.Server.ParticleAccelerator.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Imperial.Thief;
using Content.Shared.Popups;

namespace Content.Server.Imperial.Thief;
public sealed class ThiefRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedThiefRoleSystem _sharedThiefRestrictions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, GettingInteractedWithAttemptEvent>(OnAmeInteractionAttempt);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, GettingInteractedWithAttemptEvent>(OnPaInteractionAttempt);
    }

    private void OnAmeInteractionAttempt(Entity<AmeControllerComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (_sharedThiefRestrictions.CheckRestriction(ref args))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("thief-restriction-popup"), args.Uid, args.Uid, PopupType.LargeCaution);
        }
    }

    private void OnPaInteractionAttempt(Entity<ParticleAcceleratorControlBoxComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (_sharedThiefRestrictions.CheckRestriction(ref args))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("thief-restriction-popup"), args.Uid, args.Uid, PopupType.LargeCaution);
        }
    }
}
