using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Imperial.EmergencyButton.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Station;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.EmergencyButton;

public sealed class EmergencyButtonSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _sharedPopup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmergencyButtonComponent, UseInHandEvent>(OnUseInHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Обработка автоматического разпрайминга
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<EmergencyButtonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUnprime is not {} time)
                continue;

            if (now >= time)
                Unprime((uid, comp));
        }
    }

    private void OnUseInHand(EntityUid uid, EmergencyButtonComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;

        // Проверяем есть ли заряды
        if (component.CurrentCharges <= 0)
        {
            _popup.PopupEntity(Loc.GetString(component.NoChargesMessage), uid, user);
            args.Handled = true;
            return;
        }

        // Если кнопка не "заправлена" - заправляем её и отменяем действие
        if (component.NextConfirm is not {} confirm)
        {
            Prime((uid, component), user);
            args.Handled = true;
            return;
        }

        // Заправлена, но время подтверждения ещё не прошло - отменяем действие
        if (_timing.CurTime < confirm)
        {
            _sharedPopup.PopupEntity(Loc.GetString(component.ConfirmationMessage), uid, user, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        // Заправлена и время подтверждения прошло - выполняем действие
        ExecuteEmergencyAction((uid, component), user);
        args.Handled = true;
    }

    private void ExecuteEmergencyAction(Entity<EmergencyButtonComponent> entity, EntityUid user)
    {
        var (uid, component) = entity;

        // Разпрайм кнопку
        Unprime(entity);

        // Тратим заряд
        component.CurrentCharges--;
        Dirty(uid, component);

        // Получаем имя офицера
        var officerName = Identity.Name(user, EntityManager);

        // Получаем местоположение
        var location = GetUserLocation(user);

        // Формируем сообщение для рации
        var message = Loc.GetString(component.AlertMessage,
            ("officerName", officerName),
            ("location", location));

        // Отправляем сообщение в рацию
        if (_prototype.TryIndex<RadioChannelPrototype>(component.RadioChannel, out var radioChannel))
        {
            _radio.SendRadioMessage(uid, message, radioChannel, uid);
        }

        // Показываем сообщение пользователю
        _popup.PopupEntity(Loc.GetString(component.UseMessage), uid, user);
    }

    private void Prime(Entity<EmergencyButtonComponent> entity, EntityUid user)
    {
        var (uid, comp) = entity;
        comp.NextConfirm = _timing.CurTime + comp.ConfirmDelay;
        comp.NextUnprime = comp.NextConfirm + comp.PrimeTime;
        Dirty(uid, comp);

        _sharedPopup.PopupEntity(Loc.GetString(comp.ConfirmationMessage), uid, user, PopupType.LargeCaution);
    }

    private void Unprime(Entity<EmergencyButtonComponent> entity)
    {
        var (uid, comp) = entity;
        comp.NextConfirm = null;
        comp.NextUnprime = null;
        Dirty(uid, comp);
    }

    private string GetUserLocation(EntityUid user)
    {
        // Получаем ближайший видимый конфигурируемый бекон и используем его имя
        if (_navMap.TryGetNearestBeacon((user, null), out var beacon, out _))
            return beacon!.Value.Comp.Text!;

        // Бекон не найден
        return Loc.GetString("alert-emergency-button-unknown-location");
    }
}
