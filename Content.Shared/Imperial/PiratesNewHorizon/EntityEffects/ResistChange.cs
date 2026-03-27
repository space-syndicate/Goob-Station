using Content.Shared.EntityEffects;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Imperial.PiratesNewHorizon.Reagent.Components;


namespace Content.Shared.Imperial.PiratesNewHorizon.EntityEffects;


public sealed partial class ResistChangeEntityEffectSystem : EntityEffectSystem<ResistModifierMetabolismComponent, ResistChange>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;


    protected override void Effect(Entity<ResistModifierMetabolismComponent> entity, ref EntityEffectEvent<ResistChange> args)
    {
        var status = _entityManager.EnsureComponent<ResistModifierMetabolismComponent>(entity);
        var statusLifetime = args.Effect.StatusLifetime;

        status.Modifiers = args.Effect.Modifiers;
        statusLifetime = args.Effect.StatusLifetime * args.Scale;


        IncreaseTimer(status, statusLifetime, entity.Owner);
    }

    private void IncreaseTimer(ResistModifierMetabolismComponent status, float time, EntityUid uid)
    {
        var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, _gameTiming.CurTime.TotalSeconds);

        status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
        _entityManager.Dirty(uid, status);
    }
}

public sealed partial class ResistChange : EntityEffectBase<ResistChange>
{
    /// <summary>
    /// The damage reduction
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;

    /// <summary>
    /// How long the modifier applies (in seconds) when metabolized.
    /// </summary>
    [DataField]
    public float StatusLifetime = 2f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var modifiers = new List<string>();
        var gains = false;
        var loses = false;
        foreach (var coefReag in Modifiers.Coefficients)
        {
            if (coefReag.Value == 1)
                continue;
            var newcoef = FixedPoint2.Sign((1 - coefReag.Value) * 100);
            if (coefReag.Value < 1)
                gains = true;
            else
                loses = true;
            modifiers.Add(
                Loc.GetString("resistance-change-display",
                    ("kind", coefReag.Key),
                    ("amount", Math.Round((1 - coefReag.Value) * 100)),
                    ("deltasign", newcoef)
                ));
        }
        var gainsorloses = gains ? (loses ? "both" : "gains") : (loses ? "loses" : "none");
        return Loc.GetString("reagent-effect-guidebook-resistance-change",
            ("chance", Probability),
            ("changes", ContentLocalizationManager.FormatList(modifiers)),
            ("gainsorloses", gainsorloses),
            ("time", StatusLifetime));
    }
}
