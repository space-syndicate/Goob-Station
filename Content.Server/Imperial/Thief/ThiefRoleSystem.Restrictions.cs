using Content.Server.Ame.Components;
using Content.Server.ParticleAccelerator.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Imperial.Thief;

namespace Content.Server.Imperial.Thief;
public sealed class ThiefRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedThiefRoleSystem _sharedThiefRestrictions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, GettingInteractedWithAttemptEvent>(OnAmeInteractionAttempt);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, GettingInteractedWithAttemptEvent>(OnPaInteractionAttempt);
    }

    private void OnAmeInteractionAttempt(Entity<AmeControllerComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        _sharedThiefRestrictions.CheckRestriction(ref args);
    }

    private void OnPaInteractionAttempt(Entity<ParticleAcceleratorControlBoxComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        _sharedThiefRestrictions.CheckRestriction(ref args);
    }
}
