ent-WeaponDismissinator = увольнятор
    .desc = Табельное оружие главы персонала. Зарядите его авторизованной айди-картой, бланком и печатью — и вручайте документы дистанционно.
ent-BulletDismissinator = увольнительный заряд
ent-PaperDismissalNotice = приказ об увольнении
    .desc = Бланк приказа об увольнении НТ. Заполняется в момент вручения.

dismissinator-slot-id = Айди-карта
dismissinator-slot-paper = Бланк
dismissinator-slot-stamp = Печать

dismissinator-no-id = Не вставлена айди-карта!
dismissinator-no-authority = У вставленной айди-карты нет полномочий менять доступы!
dismissinator-no-paper = Не вставлен бланк!
dismissinator-no-stamp = Не вставлена печать!

dismissinator-hit-popup = Вы уволены!
dismissinator-outranked = Отказано: у цели есть допуски, которых нет на вашей карте!
dismissinator-unknown = НЕИЗВЕСТНО
dismissinator-access-none = отсутствуют

dismissinator-document-text =
    ⠀[head=3]NanoTrasen[/head]
    ⠀[head=3]ПРИКАЗ ОБ УВОЛЬНЕНИИ[/head]
    =============================================
    Станция: { $station }
    Время от начала смены и дата: { $date }

    Сотрудник: { $name }
    Должность: { $job }

    Настоящим приказом указанный сотрудник освобождается от занимаемой должности.
    Все станционные доступы аннулированы.

    Аннулированные доступы: { $access }

    Приказ издал: { $authorName }
    Должность: { $authorJob }
    =============================================

ent-PaperAccessExpansionNotice = приказ о расширении доступа
    .desc = Бланк приказа НТ о расширении доступа. Заполняется в момент вручения.

dismissinator-verb-toggle-mode = Переключить режим
dismissinator-mode-dismissal = увольнение
dismissinator-mode-expansion = расширение доступа
dismissinator-mode-switched = Режим: { $mode }.
dismissinator-examine-mode = Выставлен режим: [color=#6ec0ea]{ $mode }[/color].
dismissinator-expansion-popup = Ваши полномочия расширены!

dismissinator-expansion-document-text =
    ⠀[head=3]NanoTrasen[/head]
    ⠀[head=3]ПРИКАЗ О РАСШИРЕНИИ ДОСТУПА[/head]
    =============================================
    Станция: { $station }
    Время от начала смены и дата: { $date }

    Сотрудник: { $name }
    Должность: { $job }

    Настоящим приказом указанному сотруднику предоставляется дополнительный станционный доступ.

    Выданные доступы: { $access }

    Приказ издал: { $authorName }
    Должность: { $authorJob }
    =============================================

ent-PaperCovertDirective = служебное предписание
    .desc = Типовой бланк служебного задания НТ. Заполняется в момент вручения.

dismissinator-mode-objective = вербовка
dismissinator-objective-popup = Вам поручено новое задание!

dismissinator-objectives-none = не установлены
dismissinator-objective-document-text =
    ⠀[head=3]NanoTrasen[/head]
    ⠀[head=3]СЛУЖЕБНОЕ ПРЕДПИСАНИЕ[/head]
    =============================================
    Станция: { $station }
    Время от начала смены и дата: { $date }

    Сотрудник: { $name }

    Настоящим предписанием указанному сотруднику поручается следующее, к исполнению немедленно:

    { $objectives }

    =============================================
    ⠀[italic]Настоящее предписание имеет приоритет над всеми прочими указаниями.[/italic]
