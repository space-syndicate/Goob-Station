using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Dataset;
using Content.Server.Chat.Systems;
using Content.Server.Imperial.MTFCall;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Map; // Добавлен using

namespace Content.Server.Imperial.MTFCall;

public sealed class ShuttleMTFNameSystem : EntitySystem
{
    /* 
     * Все зависимости и поля удалены, так как они больше не используются. 
     * Если вы планируете добавить логику позже, их можно вернуть.
     */

    public override void Initialize()
    {
        base.Initialize();
        // Мы просто не подписываемся на событие ComponentStartup, 
        // поэтому нет необходимости вызывать UnsubscribeLocalEvent.
        // Если логика не нужна, этот класс может оставаться пустым.
    }
}
