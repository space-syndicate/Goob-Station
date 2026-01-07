using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Imperial.XenoGenetics.Components;


namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class SpeedModifierGeneSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeedModifierGeneComponent, GeneInsertedEvent>(OnSpeedGeneInsert);
        SubscribeLocalEvent<SpeedModifierGeneComponent, GeneWithdrawnEvent>(OnSpeedGeneWithdraw);
        SubscribeLocalEvent<SpeedModifiedByGeneComponent, RefreshMovementSpeedModifiersEvent>(RefreshMovement);
    }
    private void OnSpeedGeneInsert(EntityUid uid, SpeedModifierGeneComponent component, ref GeneInsertedEvent args)
    {
        var comp = AddComp<SpeedModifiedByGeneComponent>(args.Target);
        var geneComp = EnsureComp<XenoGeneComponent>(args.Gene);
        comp.actualSpeed = component.speedModifier + geneComp.geneMultiplier;
        /// Чтобы не было ситуаций когда ген тебя замедляет
        ///if(comp.actualSpeed <= 1)
        ///    comp.actualSpeed = 1.05f;
        /// UPD: было пиздец сильным, поэтому убрал. мышей слишком легко получить.
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.Target);
    }
    private void OnSpeedGeneWithdraw(EntityUid uid, SpeedModifierGeneComponent component, ref GeneWithdrawnEvent args)
    {    
        RemComp<SpeedModifiedByGeneComponent>(args.Target);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.Target);
    }
    private void RefreshMovement(EntityUid uid, SpeedModifiedByGeneComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.actualSpeed, component.actualSpeed);
    }
        
}