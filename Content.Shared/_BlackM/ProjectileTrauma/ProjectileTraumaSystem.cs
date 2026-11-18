using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._BlackM.ProjectileTrauma;

public sealed class ProjectileTraumaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    private const float MinimumDamage = 1f;
    private const float FullStrengthDamage = 20f;
    private const float MaximumWalkModifier = 0.35f;
    private const float MinimumWalkModifier = 0.20f;
    private const float MaximumSprintModifier = 0.20f;
    private const float MinimumSprintModifier = 0.10f;
    private const double RecoverySeconds = 2.0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileTraumaComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<ProjectileTraumaComponent, ComponentShutdown>(OnShutdown);
    }

    public void ApplyHit(EntityUid target, DamageSpecifier? appliedDamage)
    {
        if (_net.IsClient || TerminatingOrDeleted(target) || appliedDamage == null
            || !HasComp<HumanoidAppearanceComponent>(target)
            || !HasComp<MovementSpeedModifierComponent>(target)
            || !TryComp<MobStateComponent>(target, out var mob) || mob.CurrentState != MobState.Alive)
            return;

        var damage = (float) appliedDamage.GetTotal();
        if (damage < MinimumDamage)
            return;

        var now = _timing.CurTime;
        var comp = EnsureComp<ProjectileTraumaComponent>(target);
        GetModifiers(comp, now, out var currentWalk, out var currentSprint);
        var severity = Math.Clamp(damage / FullStrengthDamage, 0f, 1f);
        var walk = MaximumWalkModifier - (MaximumWalkModifier - MinimumWalkModifier) * severity;
        var sprint = MaximumSprintModifier - (MaximumSprintModifier - MinimumSprintModifier) * severity;
        comp.WalkModifier = Math.Min(currentWalk, walk);
        comp.SprintModifier = Math.Min(currentSprint, sprint);

        var holdUntil = now + TimeSpan.FromSeconds(2.0 + severity);
        if (holdUntil > comp.HoldUntil)
            comp.HoldUntil = holdUntil;
        comp.RecoverUntil = comp.HoldUntil + TimeSpan.FromSeconds(RecoverySeconds);
        Dirty(target, comp);
        _movement.RefreshMovementSpeedModifiers(target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ProjectileTraumaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (MetaData(uid).EntityPaused)
                continue;

            GetModifiers(comp, now, out var walk, out var sprint);
            var finished = now >= comp.RecoverUntil;
            if (!finished && Math.Abs(walk - comp.LastWalkModifier) < 0.01f
                && Math.Abs(sprint - comp.LastSprintModifier) < 0.01f)
                continue;
            if (finished && comp.LastWalkModifier == 1f && comp.LastSprintModifier == 1f)
                continue;

            comp.LastWalkModifier = walk;
            comp.LastSprintModifier = sprint;
            _movement.RefreshMovementSpeedModifiers(uid);
        }
    }

    private void OnRefresh(EntityUid uid, ProjectileTraumaComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (comp.LifeStage >= ComponentLifeStage.Stopping)
            return;
        GetModifiers(comp, _timing.CurTime, out var walk, out var sprint);
        args.ModifySpeed(walk, sprint);
    }

    private void OnShutdown(EntityUid uid, ProjectileTraumaComponent comp, ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(uid))
            _movement.RefreshMovementSpeedModifiers(uid);
    }

    private static void GetModifiers(ProjectileTraumaComponent comp, TimeSpan now, out float walk, out float sprint)
    {
        var remaining = comp.RecoverUntil - comp.HoldUntil;
        var recovery = now <= comp.HoldUntil ? 0f : remaining <= TimeSpan.Zero ? 1f
            : (float) Math.Clamp((now - comp.HoldUntil).TotalSeconds / remaining.TotalSeconds, 0, 1);
        walk = comp.WalkModifier + (1f - comp.WalkModifier) * recovery;
        sprint = comp.SprintModifier + (1f - comp.SprintModifier) * recovery;
    }
}
