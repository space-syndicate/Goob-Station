using Content.Shared.Mobs.Components;
using Robust.Shared.Physics.Events;
using Content.Server.GameTicking;
using Robust.Server.Player;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Imperial.XxRaay.SyndieBattle;

/// <summary>
/// Отправляет игрока в лобби при касании того у кого есть компонент.
/// </summary>
public sealed class RespawnTouchSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RespawnTouchComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, RespawnTouchComponent component, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!HasComp<MobStateComponent>(other))
            return;

        if (TryComp<ActorComponent>(other, out var actor))
        {
            _ticker.Respawn(actor.PlayerSession);

            if (component.DeleteBody)
            {
                QueueDel(other);
            }
        }
    }
}
