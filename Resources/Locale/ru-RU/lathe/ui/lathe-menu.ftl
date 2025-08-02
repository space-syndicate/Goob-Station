lathe-menu-title = Меню станка
lathe-menu-queue = Очередь
lathe-menu-server-list = Список серверов
lathe-menu-sync = Синхр.
lathe-menu-search-designs = Поиск чертежей
lathe-menu-category-all = Все
lathe-menu-search-filter = Фильтр:
lathe-menu-amount = Количество:
lathe-menu-recipe-count = { $count ->
[1] {$count} чертеж
[few] {$count} чертежа
*[other] {$count} чертежей
}
lathe-menu-reagent-slot-examine = Имеется слот для мензурки сбоку.
lathe-reagent-dispense-no-container = Жидкость выливается из {THE($name)} на пол!
lathe-menu-result-reagent-display = {$reagent} ({$amount}ед.)
lathe-menu-material-display = {$material} ({$amount})
lathe-menu-tooltip-display = {$amount} {$material}
lathe-menu-description-display = [italic]{$description}[/italic]
lathe-menu-material-amount = { $amount ->
[1] {NATURALFIXED($amount, 2)} {$unit}
*[other] {NATURALFIXED($amount, 2)} {MAKEPLURAL($unit)}
}
lathe-menu-material-amount-missing = { $amount ->
[1] {NATURALFIXED($amount, 2)} {$unit} {$material} ([color=red]{NATURALFIXED($missingAmount, 2)} {$unit} не хватает[/color])
*[other] {NATURALFIXED($amount, 2)} {MAKEPLURAL($unit)} {$material} ([color=red]{NATURALFIXED($missingAmount, 2)} {MAKEPLURAL($unit)} не хватает[/color])
}
lathe-menu-no-materials-message = Материалы не загружены
lathe-menu-silo-linked-message = Склад подключен
lathe-menu-fabricating-message = Производство...
lathe-menu-materials-title = Материалы
lathe-menu-queue-title = Очередь производства
