using System.Text.Json.Serialization;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Resurrection of an entity with the instant return of the ghost to the body.
/// </summary>
public sealed partial class Resurrection : EntityEffectBase<Resurrection>
{
    [DataField]
    public DamageSpecifier PenaltyDamage = new()!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-resurrection",
            ("chance", Probability)
        );
}
