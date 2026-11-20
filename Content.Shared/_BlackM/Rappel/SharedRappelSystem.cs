using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Robust.Shared.Timing;

namespace Content.Shared._BlackM.Rappel;

public abstract class SharedRappelSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RappellingComponent, ComponentStartup>(OnRappelStartup);
        SubscribeLocalEvent<RappellingComponent, ComponentShutdown>(OnRappelShutdown);
        SubscribeLocalEvent<RappellingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<RappellingComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<RappellingComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<RappellingComponent, FlashAttemptEvent>(OnFlashAttempt);
        SubscribeLocalEvent<RappellingComponent, PreventCollideEvent>(OnRappelCollide);
        SubscribeLocalEvent<RappellingComponent, ThrowHitByEvent>(OnThrownHit);

        SubscribeLocalEvent<RappelRopeComponent, ExaminedEvent>(OnRopeExamined);
    }

    protected virtual void OnRappelStartup(Entity<RappellingComponent> ent, ref ComponentStartup args)
    {
        _blocker.UpdateCanMove(ent);
    }

    protected virtual void OnRappelShutdown(Entity<RappellingComponent> ent, ref ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnUpdateCanMove(EntityUid uid, RappellingComponent comp, UpdateCanMoveEvent args)
    {
        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnInteractionAttempt(Entity<RappellingComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnAttackAttempt(EntityUid uid, RappellingComponent comp, AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnFlashAttempt(Entity<RappellingComponent> ent, ref FlashAttemptEvent args)
    {
        if (!ent.Comp.Preparing && ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancelled = true;
    }

    private void OnRappelCollide(Entity<RappellingComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.Preparing || ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;
        if (HasComp<ThrownItemComponent>(args.OtherEntity) ||
            HasComp<RappellingComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnThrownHit(Entity<RappellingComponent> ent, ref ThrowHitByEvent args)
    {
        if (!ent.Comp.Preparing && ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Handled = true;
    }

    private void OnRopeExamined(Entity<RappelRopeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Linked
            ? "rappel-rope-examine-linked"
            : "rappel-rope-examine-unlinked"));
    }

    public static float GetProgress(RappellingComponent comp, TimeSpan now)
    {
        if (comp.Duration <= 0f)
            return 1f;

        return Math.Clamp((float) ((now - comp.StartTime).TotalSeconds / comp.Duration), 0f, 1f);
    }

    public static float GetDescentTravel(float progress)
    {
        var p = Math.Clamp(progress, 0f, 1f);
        const float acceleration = 0.18f;
        const float braking = 0.28f;
        const float cruiseEnd = 1f - braking;
        const float distance = 1f - (acceleration + braking) * 0.5f;

        if (p < acceleration)
        {
            var u = p / acceleration;
            return acceleration * (u * u * u - 0.5f * u * u * u * u) / distance;
        }

        if (p <= cruiseEnd)
            return (p - acceleration * 0.5f) / distance;

        var remaining = (1f - p) / braking;
        return 1f - braking * (remaining * remaining * remaining -
            0.5f * remaining * remaining * remaining * remaining) / distance;
    }

    public static readonly Vector2 HarnessOffset = new(0.00f, 0.10f);
    public static readonly Vector2 RopeTailOffset = new(0.00f, -0.35f);

    public static Vector2 GetDescentOffset(RappellingComponent comp, TimeSpan now)
    {
        var p = GetProgress(comp, now);
        return new Vector2(0f, MathF.Max(0f, comp.Height) * (1f - GetDescentTravel(p)));
    }
}
