using Content.Server.Administration.Systems;
using Content.Server.Flash;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Imperial.ChemistryRework;

public sealed class ImperialEntityEffectSystem : EntitySystem
{
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly FlashSystem _flashSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly GravityWellSystem _gravityWellSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenateSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecuteEntityEffectEvent<RemoveMark>>(OnExecuteRemoveMark);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<Resurrection>>(OnExecuteResurrection);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<GravityReactionEffect>>(OnExecuteGravityReactionEffect);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ChangeMarkingColor>>(OnExecuteChangeMarkingColor);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ImperialFlashReactionEffect>>(OnExecuteImperialFlashReactionEffect);
    }

    private void OnExecuteRemoveMark(ref ExecuteEntityEffectEvent<RemoveMark> args)
    {
        if (!Enum.TryParse(args.Effect.MarkingCategory, out Shared.Humanoid.Markings.MarkingCategories marking)) return;

        _humanoidAppearanceSystem.RemoveMarking(args.Args.TargetEntity, marking, 0);
    }

    private void OnExecuteResurrection(ref ExecuteEntityEffectEvent<Resurrection> args)
    {
        _rejuvenateSystem.PerformRejuvenate(args.Args.TargetEntity);

        _damageableSystem.TryChangeDamage(
            args.Args.TargetEntity,
            args.Effect.PenaltyDamage,
            true
        );

        if (_mindSystem.TryGetMind(args.Args.TargetEntity, out var mindUid, out var mind))
            _mindSystem.UnVisit(mindUid, mind);

        _popupSystem.PopupEntity(Loc.GetString("reagent-effect-body-resurrection"), args.Args.TargetEntity, PopupType.LargeCaution);
    }

    private void OnExecuteGravityReactionEffect(ref ExecuteEntityEffectEvent<GravityReactionEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs reagentArgs) return;

        var range = MathF.Min((float)(reagentArgs.Quantity * args.Effect.ImpulsePerUnit), args.Effect.MaxRange);

        _gravityWellSystem.GravPulse(
            args.Args.TargetEntity,
            range,
            args.Effect.MinRange,
            args.Effect.BaseRadialDeltaV,
            args.Effect.BaseTangentialDeltaV
        );
    }

    private void OnExecuteChangeMarkingColor(ref ExecuteEntityEffectEvent<ChangeMarkingColor> args)
    {
        if (!Enum.TryParse(args.Effect.MarkingCategory, out Shared.Humanoid.Markings.MarkingCategories marking) && args.Effect.MarkingCategory != "Skin") return;

        var color = args.Effect.InvertColor ? args.Effect.InvertMarkingColor(args.Args, marking) : args.Effect.GenerateColor();

        if (args.Effect.MarkingCategory == "Skin")
            _humanoidAppearanceSystem.SetSkinColor(args.Args.TargetEntity, color);
        else
            _humanoidAppearanceSystem.SetMarkingColor(args.Args.TargetEntity, marking, 0, new List<Color> { color });
    }

    private void OnExecuteImperialFlashReactionEffect(ref ExecuteEntityEffectEvent<ImperialFlashReactionEffect> args)
    {

        var transform = Comp<TransformComponent>(args.Args.TargetEntity);
        var uid = Spawn(args.Effect.FlashEffectPrototype, _transformSystem.GetMapCoordinates(transform));

        var range = 1f;

        _transformSystem.AttachToGridOrMap(uid);

        if (TryComp<SharedPointLightComponent>(uid, out var pointLightComp))
            _pointLightSystem.SetRadius(uid, MathF.Max(1.1f, range), pointLightComp);

        if (args.Args is not EntityEffectReagentArgs reagentArgs)
        {
            if (args.Effect.SlowOnlyTarget)
            {
                _flashSystem.Flash(
                    args.Args.TargetEntity,
                    null,
                    null,
                    args.Effect.MaxDuration * 1000f,
                    1.0f
                );

                return;
            }

            _flashSystem.FlashArea(
                args.Args.TargetEntity,
                null,
                args.Effect.MaxRange,
                args.Effect.MaxDuration * 1000f,
                args.Effect.SlowTo
            );

            return;
        }

        range = MathF.Min((float)(reagentArgs.Quantity * args.Effect.PowerPerUnit), args.Effect.MaxRange);
        var duration = MathF.Min((float)(reagentArgs.Quantity * args.Effect.PowerPerUnit), args.Effect.MaxDuration) * 1000f;

        if (args.Effect.SlowOnlyTarget)
        {
            _flashSystem.Flash(
                args.Args.TargetEntity,
                null,
                null,
                duration * 1000f,
                args.Effect.SlowTo
            );

            return;
        }

        _flashSystem.FlashArea(
            args.Args.TargetEntity,
            null,
            range,
            duration,
            args.Effect.SlowTo
        );
    }
}
