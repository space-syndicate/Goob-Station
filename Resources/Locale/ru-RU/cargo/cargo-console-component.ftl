## UI
cargo-console-menu-title = Консоль заказа грузов
cargo-console-menu-account-name-label = Аккаунт:{" "}
cargo-console-menu-account-name-none-text = Нет
cargo-console-menu-account-name-format = [bold][color={$color}]{$name}[/color][/bold] [font="Monospace"]\[{$code}\][/font]
cargo-console-menu-shuttle-name-label = Название шаттла:{" "}
cargo-console-menu-shuttle-name-none-text = Нет
cargo-console-menu-points-label = Баланс:{" "}
cargo-console-menu-points-amount = ${$amount}
cargo-console-menu-shuttle-status-label = Статус шаттла:{" "}
cargo-console-menu-shuttle-status-away-text = Отбыл
cargo-console-menu-order-capacity-label = Объём заказов:{" "}
cargo-console-menu-call-shuttle-button = Активировать телепад
cargo-console-menu-permissions-button = Доступы
cargo-console-menu-categories-label = Категории:{" "}
cargo-console-menu-search-bar-placeholder = Поиск
cargo-console-menu-requests-label = Запросы
cargo-console-menu-orders-label = Заказы
cargo-console-menu-order-reason-description = Причина: {$reason}
cargo-console-menu-populate-categories-all-text = Все
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = {$productName} (x{ $orderAmount }) от { $orderRequester } с счета [color={$accountColor}]{$account}[/color]
cargo-console-menu-cargo-order-row-approve-button = Одобрить
cargo-console-menu-cargo-order-row-cancel-button = Отказать
cargo-console-menu-tab-title-orders = Заказы
cargo-console-menu-tab-title-funds = Переводы
cargo-console-menu-account-action-transfer-limit = [bold]Лимит перевода:[/bold] ${$limit}
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Безлимитно)[/color]
cargo-console-menu-account-action-select = [bold]Действие по счету:[/bold]
cargo-console-menu-account-action-amount = [bold]Количество:[/bold] $
cargo-console-menu-account-action-button = Перевести
cargo-console-menu-toggle-account-lock-button = Переключить лимит перевода
cargo-console-menu-account-action-option-withdraw = Вывести кредиты
cargo-console-menu-account-action-option-transfer = Перевести кредиты на счет {$code}

# Orders
cargo-console-order-not-allowed = Доступ не обнаружен
cargo-console-station-not-found = Станция не обнаружена
cargo-console-invalid-product = Неверный ID продукта
cargo-console-too-many = Лимит по одобренным заказам превышен
cargo-console-snip-snip = Заказ был уреза для вместимости на шаттле
cargo-console-insufficient-funds = Недостаточно средств (требуется {$cost})
cargo-console-unfulfilled = Нет места для выполнения заказа
cargo-console-trade-station = Отправлено в {$destination}
cargo-console-unlock-approved-order-broadcast = [bold]{$productName} x{$orderAmount}[/bold], который стоит  [bold]{$cost}[/bold], был одобрен [bold]{$approver}[/bold]
cargo-console-fund-withdraw-broadcast = [bold]{$name} вывел {$amount} кредитов со счета {$name1} \[{$code1}\]
cargo-console-fund-transfer-broadcast = [bold]{$name} перевел {$amount} кредитов из счета {$name1} \[{$code1}\] на {$name2} \[{$code2}\][/bold]
cargo-console-fund-transfer-user-unknown = Неизвестный

cargo-console-paper-reason-default = Отсутствует
cargo-console-paper-approver-default = Пользователь
cargo-console-paper-print-name = Заказ #{$orderNumber}
cargo-console-paper-print-text = [head=2]Заказ #{$orderNumber}[/head]
    {"[bold]Запрос:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Запросил:[/bold]"} {$requester}

    {"[head=3]Информация заказа[/head]"}
    {"[bold]Покупатель[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Принята:[/bold]"} {$approver}
    {"[bold]Причина:[/bold]"} {$reason}

# Cargo shuttle console
cargo-shuttle-console-menu-title = Консоль вызова шаттла снабжения
cargo-shuttle-console-station-unknown = Неизвестно
cargo-shuttle-console-shuttle-not-found = Не найден
cargo-shuttle-console-organics = На шаттле обнаружены органические формы жизни
cargo-no-shuttle = Шаттл снабжения не обнаружен!

# Funding allocation console
cargo-funding-alloc-console-menu-title = Консоль распределения средств
cargo-funding-alloc-console-label-account = [bold]Счёт[/bold]
cargo-funding-alloc-console-label-code = [bold] Код [/bold]
cargo-funding-alloc-console-label-balance = [bold] Баланс [/bold]
cargo-funding-alloc-console-label-cut = [bold] Распределение доходов (%) [/bold]

cargo-funding-alloc-console-label-primary-cut = Доля карго от средств из не-сейфовых источников (%):
cargo-funding-alloc-console-label-lockbox-cut = Доля карго от продаж из сейфа (%):

cargo-funding-alloc-console-label-help-non-adjustible = Карго получает {$percent}% прибыли от не-сейфовых продаж. Остальное распределяется как указано ниже:
cargo-funding-alloc-console-label-help-adjustible = Оставшиеся средства из не-сейфовых источников распределяются как указано ниже:
cargo-funding-alloc-console-button-save = Сохранить изменения
cargo-funding-alloc-console-label-save-fail = [bold]Распределение доходов неверно![/bold] [color=red]({$pos ->
    [1] +
    *[-1] -
}{$val}%)[/color]

# Slip template
cargo-acquisition-slip-body = [head=3]Детали актива[/head]
    {"[bold]Продукт:[/bold]"} {$product}
    {"[bold]Описание:[/bold]"} {$description}
    {"[bold]Цена за единицу:[/bold]"} ${$unit}
    {"[bold]Количество:[/bold]"} {$amount}
    {"[bold]Стоимость:[/bold]"} ${$cost}

    {"[head=3]Детали покупки[/head]"}
    {"[bold]Заказчик:[/bold]"} {$orderer}
    {"[bold]Причина:[/bold]"} {$reason}
