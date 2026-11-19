ent-AmsMachine = антимасс-спектрометр
    .desc = Гигантская резонансная установка. Установите все шесть компонентов, закрепите их материалами, поднимите рычаги и вкатите в луч тележку с образцом.
ent-AmsSampleCart = тележка с образцом
    .desc = Платформа с кристаллом-образцом для антимасс-спектрометра. Вкатите её в луч, чтобы начать синхронизацию.
ent-AmsXenPortal = портал в Зен
    .desc = Разрыв пространства. Лучше держаться подальше.

ent-AmsPartCore = ядро АМС
    .desc = Ключевой резонансный компонент антимасс-спектрометра.
ent-AmsPartCoil = катушка АМС
    .desc = Индукционная катушка антимасс-спектрометра.
ent-AmsPartLens = фокусирующая линза АМС
    .desc = Прецизионная линза для фокусировки поля.
ent-AmsPartCell = энергоячейка АМС
    .desc = Компактный источник питания для запуска синхронизации.
ent-AmsPartStabilizer = стабилизатор АМС
    .desc = Удерживает поле в стабильном состоянии на время работы.
ent-AmsPartCasing = корпус АМС
    .desc = Защитный корпус, замыкающий контур установки.

ams-part-core = Ядро
ams-part-coil = Катушка
ams-part-lens = Линза
ams-part-cell = Энергоячейка
ams-part-stabilizer = Стабилизатор
ams-part-casing = Корпус

ams-ui-title = Антимасс-спектрометр
ams-ui-status = Состояние
ams-ui-parts-ready = Готово: { $ready }/{ $total }
ams-ui-timer-synchronizing = До включения поля
ams-ui-timer-active = До закрытия поля
ams-ui-timer-detonating = До коллапса
ams-ui-parts = Компоненты
ams-ui-charge = Заряд синхронизации

ams-ui-status-inactive = Неактивен
ams-ui-status-synchronizing = Синхронизация
ams-ui-status-ready = Готов к активации
ams-ui-status-active = Поле стабильно
ams-ui-status-detonating = Каскадный распад
ams-ui-status-destroyed = Уничтожен

ams-ui-part-missing = Не установлено
ams-ui-part-reinforce = { $material } { $done }/{ $needed }
ams-ui-part-secured = Поднимите рычаг
ams-ui-part-ready = Готово
ams-ui-lever-tooltip = Рычаг компонента
ams-ui-lever-on = Вкл
ams-ui-lever-off = Выкл

ams-ui-sample = Образец:
ams-ui-sample-missing = не в луче
ams-ui-sample-in-place = в луче
ams-ui-sample-locked = зафиксирован

ams-ui-hint-inactive = Вставьте все шесть компонентов в слоты машины.
ams-ui-hint-reinforce = Закрепите компоненты материалами — нажмите по машине стопкой нужного материала.
ams-ui-hint-levers = Поднимите рычаги всех компонентов.
ams-ui-hint-sample = Вкатите тележку с образцом в нижнюю часть луча — на подсвеченный тайл.
ams-ui-hint-primed = Установка готова к синхронизации.
ams-ui-hint-synchronizing = Идёт накопление энергии. После синхронизации поле включится само.
ams-ui-hint-ready = Синхронизация завершена.
ams-ui-hint-active = Эвакуационные лифты работают.
ams-ui-hint-detonating = Поле потеряло устойчивость. Взрыв неизбежен.
ams-ui-hint-destroyed = Установка разрушена.

ams-ui-btn-need-parts = Установите все компоненты
ams-ui-btn-need-reinforce = Закрепите все компоненты
ams-ui-btn-need-levers = Поднимите все рычаги
ams-ui-btn-need-sample = Вкатите тележку с образцом
ams-ui-btn-start-sync = Начать синхронизацию
ams-ui-btn-synchronizing = Синхронизация…
ams-ui-btn-ready = Активация…
ams-ui-btn-active = Поле активно
ams-ui-btn-detonating = Распад необратим
ams-ui-btn-destroyed = Установка уничтожена

ams-material-Steel = сталь
ams-material-Glass = стекло
ams-material-Plastic = пластик
ams-material-Plasteel = пласталь

ams-popup-nothing-to-reinforce = Этим сейчас нечего закреплять.
ams-popup-reinforce-progress = { $part }: осталось закрепить ещё { $left }.
ams-popup-secured = { $part } надёжно закреплён(а).

ams-announce-sender = Контроль АМС
ams-announce-sync-started = Запущена последовательность синхронизации антимасс-спектрометра. Для накопления энергии в комплексе будет использовано аварийное освещение. Ориентировочное время завершения: { $minutes } мин.
ams-announce-primed-sample = Все компоненты антимасс-спектрометра закреплены и настроены. Доставьте образец в луч для начала синхронизации.
ams-announce-primed = Все компоненты антимасс-спектрометра закреплены и настроены. Установка готова к синхронизации.
ams-announce-launched = Антимасс-спектрометр активирован. Поле останется стабильным в течение { $minutes } мин.
ams-announce-closing-soon = Внимание: поле антимасс-спектрометра дестабилизируется. Закрытие через 15 секунд.
ams-announce-evac-available = Внимание всему персоналу! Эвакуационный лифт запущен, местонахождение: { $location }. У вас есть считанные минуты, чтобы покинуть комплекс. Повторяю: немедленно покиньте комплекс.

ams-announce-cascade-start = ВНИМАНИЕ! Поле антимасс-спектрометра потеряло стабильность. Запущен необратимый каскадный резонанс. Лифт эвакуации закрыт. Вас предупреждали — спасать опоздавших никто не станет. Критический коллапс и уничтожение комплекса через { $seconds } секунд.
ams-announce-cascade-30 = До детонации антимасс-спектрометра осталось 30 секунд.
ams-announce-cascade-10 = ДЕСЯТЬ СЕКУНД ДО ДЕТОНАЦИИ АНТИМАСС-СПЕКТРОМЕТРА.
ams-announce-detonated = Каскадный резонанс прошёл через весь комплекс. Выживших не обнаружено.

ams-announce-hecu-sender = Неизвестная передача
ams-announce-hecu-intercept = Внимание персоналу комплекса. Объект и системы связи перешли под контроль HECU. Выход в эфир запрещён и карается как измена. Всем оставаться на своих местах и не препятствовать работе зачистных групп. Сотрудничество — ваш единственный шанс на «выживание».

cargoproduct-category-name-ams = Части АМС
research-technology-ams-parts = Компоненты АМС
