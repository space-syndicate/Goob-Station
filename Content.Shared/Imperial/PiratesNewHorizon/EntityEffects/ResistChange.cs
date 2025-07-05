using Content.Shared.EntityEffects;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Timing;
using Content.Shared.Imperial.PiratesNewHorizon.Reagent.Components;


namespace Content.Shared.Imperial.PiratesNewHorizon.EntityEffects
{
    [UsedImplicitly]
    public sealed partial class ResistChange : EntityEffect
    {
        /// <summary>
        /// The damage reduction
        /// </summary>
        [DataField(required: true)]
        [JsonPropertyName("modifiers")]
        public DamageModifierSet Modifiers = default!;
        /// <summary>
        /// How long the modifier applies (in seconds) when metabolized.
        /// </summary>
        [DataField]
        public float StatusLifetime = 2f;
        protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
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

        public override void Effect(EntityEffectBaseArgs args)
        {
            var status = args.EntityManager.EnsureComponent<ResistModifierMetabolismComponent>(args.TargetEntity);
            var statusLifetime = StatusLifetime;
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                status.Modifiers = Modifiers;
                statusLifetime = StatusLifetime * reagentArgs.Scale.Float();
            }

            IncreaseTimer(status, statusLifetime, args.TargetEntity, args);
        }
        public void IncreaseTimer(ResistModifierMetabolismComponent status, float time, EntityUid uid, EntityEffectBaseArgs args)
        {
            var gameTiming = IoCManager.Resolve<IGameTiming>();

            var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, gameTiming.CurTime.TotalSeconds);

            status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
            status.Dirty();
            args.EntityManager.Dirty(uid, status);
        }
    }
}
