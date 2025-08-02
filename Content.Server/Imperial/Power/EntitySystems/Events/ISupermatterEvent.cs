using Robust.Shared.GameObjects;
using System;
using Content.Server.Imperial.Power.Components;

namespace Content.Server.Imperial.Power.EntitySystems.Events;

/// <summary>
/// Базовый интерфейс для событий суперматерии
/// </summary>
public interface ISupermatterEvent
{
    /// <summary>
    /// Активирует событие
    /// </summary>
    void Activate(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system);

    /// <summary>
    /// Обрабатывает событие во время его выполнения
    /// </summary>
    void Process(EntityUid uid, SupermatterEventComponent comp, SupermatterEventSystem system, TimeSpan currentTime);

    /// <summary>
    /// Возвращает сообщение для объявления события
    /// </summary>
    string GetAnnouncement();
}
