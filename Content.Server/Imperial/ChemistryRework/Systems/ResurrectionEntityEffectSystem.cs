using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.Administration.Systems;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;

namespace Content.Server.Imperial.ChemistryRework;


public sealed partial class ResurrectionEntityEffectSystem : EntityEffectSystem<DamageableComponent, Resurrection>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenateSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;


    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<Resurrection> args)
    {
        _rejuvenateSystem.PerformRejuvenate(entity);

        _damageableSystem.TryChangeDamage(
            entity.Owner,
            args.Effect.PenaltyDamage,
            true
        );

        if (_mindSystem.TryGetMind(entity, out var mindUid, out var mind))
            _mindSystem.UnVisit(mindUid, mind);

        _popupSystem.PopupEntity(Loc.GetString("reagent-effect-body-resurrection"), entity, PopupType.LargeCaution);
    }
}
