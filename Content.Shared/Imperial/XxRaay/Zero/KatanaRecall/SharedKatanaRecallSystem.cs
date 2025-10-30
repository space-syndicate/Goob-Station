using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.XxRaay.Zero.KatanaRecall;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Zero.KatanaRecall;

/// <summary>
/// Shared system for katana recall functionality.
/// Handles common logic between client and server.
/// </summary>
public abstract class SharedKatanaRecallSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KatanaRecallComponent, ComponentStartup>(OnKatanaRecallStartup);
        SubscribeLocalEvent<KatanaRecallComponent, ComponentShutdown>(OnKatanaRecallShutdown);
    }

    private void OnKatanaRecallStartup(Entity<KatanaRecallComponent> entity, ref ComponentStartup args)
    {
        // Add the recall action to the katana
        Actions.AddAction(entity.Owner, ref entity.Comp.ActionEntity, "ActionKatanaRecall");
    }

    private void OnKatanaRecallShutdown(Entity<KatanaRecallComponent> entity, ref ComponentShutdown args)
    {
        // Remove the recall action when component is removed
        Actions.RemoveAction(entity.Owner, entity.Comp.ActionEntity);
    }

    /// <summary>
    /// Checks if the katana recall is on cooldown.
    /// </summary>
    protected bool IsOnCooldown(Entity<KatanaRecallComponent> entity)
    {
        var component = entity.Comp;
        var currentTime = GameTiming.CurTime;

        if (component.LastRecallTime == null)
            return false;

        return currentTime < component.LastRecallTime.Value + TimeSpan.FromSeconds(component.RecallCooldown);
    }
}
