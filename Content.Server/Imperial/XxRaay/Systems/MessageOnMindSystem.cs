using Content.Server.Chat.Managers;
using Content.Server.Imperial.XxRaay.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// Система для отправления сообщения при добавлении майнда существу
/// </summary>
public sealed class MessageOnMindSystem : EntitySystem
{

    [Dependency] private readonly IChatManager _chatManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessageOnMindComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, MessageOnMindComponent comp, MindAddedMessage args)
    {
        if (comp.SentMessage)
            return;

        SendMessage(uid, comp);
        comp.SentMessage = true;
    }

    private void SendMessage(EntityUid uid, MessageOnMindComponent comp)
    {
        if (!EntityManager.TryGetComponent<ActorComponent>(uid, out var actor))
            return;

        _chatManager.DispatchServerMessage(actor.PlayerSession, comp.Message);

    }
}


