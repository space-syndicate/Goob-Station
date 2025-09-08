## Faction Shop System

faction-shop-examine = Магазин фракции { $faction }
faction-shop-points-info = { $points ->
    [1] { $points } очко за флаг каждую { $interval ->
        [1] { $interval } секунду
        [2] { $interval } секунды
        [3] { $interval } секунды
        [4] { $interval } секунды
       *[other] { $interval } секунд
    }
    [2] { $points } очка за флаг каждые { $interval ->
        [1] { $interval } секунду
        [2] { $interval } секунды
        [3] { $interval } секунды
        [4] { $interval } секунды
       *[other] { $interval } секунд
    }
    [3] { $points } очка за флаг каждые { $interval ->
        [1] { $interval } секунду
        [2] { $interval } секунды
        [3] { $interval } секунды
        [4] { $interval } секунды
       *[other] { $interval } секунд
    }
    [4] { $points } очка за флаг каждые { $interval ->
        [1] { $interval } секунду
        [2] { $interval } секунды
        [3] { $interval } секунды
        [4] { $interval } секунды
       *[other] { $interval } секунд
    }
   *[other] { $points } очков за флаг каждые { $interval ->
        [1] { $interval } секунду
        [2] { $interval } секунды
        [3] { $interval } секунды
        [4] { $interval } секунды
       *[other] { $interval } секунд
    }
}
faction-shop-opened = { $player } открыл магазин фракции { $faction }
faction-points-awarded = Фракция { $faction } получила { $points ->
    [1] { $points } очко
    [2] { $points } очка
    [3] { $points } очка
    [4] { $points } очка
   *[other] { $points } очков
} (всего: { $total ->
    [1] { $total } очко
    [2] { $total } очка
    [3] { $total } очка
    [4] { $total } очка
   *[other] { $total } очков
}, флагов: { $flags ->
    [1] { $flags } флаг
    [2] { $flags } флага
    [3] { $flags } флага
    [4] { $flags } флага
   *[other] { $flags } флагов
})

## Currency display names
faction-currency-nt = Очки НТ
faction-currency-sindi = Очки Синдиката
faction-currency-green = Очки Зеленых
faction-currency-yellow = Очки Желтых
faction-currency-red = Очки Красных
faction-currency-blue = Очки Синих
faction-currency-ussp = Очки USSP

## UI Strings
faction-shop-title = Магазин фракции
faction-shop-welcome = Добро пожаловать в магазин фракции!
faction-shop-points = У вас { $points ->
    [1] { $points } очко
    [2] { $points } очка
    [3] { $points } очка
    [4] { $points } очка
   *[other] { $points } очков
}
faction-shop-close = Закрыть
faction-shop-cost = Стоимость: { $cost ->
    [1] { $cost } очко
    [2] { $cost } очка
    [3] { $cost } очка
    [4] { $cost } очка
   *[other] { $cost } очков
}
faction-shop-buy = Купить
faction-shop-purchase = Покупка { $item } за { $cost ->
    [1] { $cost } очко
    [2] { $cost } очка
    [3] { $cost } очка
    [4] { $cost } очка
   *[other] { $cost } очков
}

## Store titles
faction-store-title-nt = Магазин НТ
faction-store-title-sindi = Магазин Синдиката
faction-store-title-green = Магазин Зеленых
faction-store-title-yellow = Магазин Желтых
faction-store-title-red = Магазин Красных
faction-store-title-blue = Магазин Синих
faction-store-title-ussp = Магазин USSP

## Shop consoles (entity names)
ent-NTShopConsole = Консоль магазина НТ
    .desc = Консоль магазина NanoTrasen
ent-SindiShopConsole = Консоль магазина Синдиката
    .desc = Консоль для покупок фракции Синдиката
ent-GreenShopConsole = Консоль магазина Зеленых
    .desc = Консоль для покупок Зеленой фракции
ent-YellowShopConsole = Консоль магазина Желтых
    .desc = Консоль для покупок Желтой фракции
ent-RedShopConsole = Консоль магазина Красных
    .desc = Консоль для покупок Красной фракции
ent-BlueShopConsole = Консоль магазина Синих
    .desc = Консоль для покупок Синей фракции
ent-USSPShopConsole = Консоль магазина USSP
    .desc = Консоль для покупок фракции USSP

## Shop Categories
faction-store-category-weapons = Оружие
faction-store-category-weapons-desc = Вооружение для вашей фракции
faction-store-category-ammo = Патроны
faction-store-category-ammo-desc = Боеприпасы к оружию
faction-store-category-medical = Медицина
faction-store-category-medical-desc = Лекарства и перевязки
faction-store-category-tools = Снаряжение
faction-store-category-tools-desc = Снаряжение и экипировка

## Shop Items
lecter = Лектер
lecter-desc = Военная штурмовая винтовка (.20)
drozd = Дрозд
drozd-desc = Пистолет-пулемёт (.35)
l6-saw = L6 SAW
l6-saw-desc = Ручной пулемёт (.30 ленты)
akms = АКМС
akms-desc = Классический автомат (.30)
ammo-light-rifle = Магазин .20/.30 винтовочный
ammo-light-rifle-desc = Стандартный винтовочный магазин
ammo-smg = Магазин .35 ПП
ammo-smg-desc = Магазин для ПП
ammo-l6-belt = Лента .30
ammo-l6-belt-desc = Патронная лента для L6

## Medical
faction-medkit = Аптечка
faction-medkit-desc = Набор первой помощи
faction-gauze = Бинты
faction-gauze-desc = Для остановки кровотечений
faction-ointment = Мазь
faction-ointment-desc = Для лечения ожогов
faction-bruise-pack = Пакет для ушибов
faction-bruise-pack-desc = Лечит ушибы

## Armor
faction-armor-bulletproof = Бронежилет
faction-armor-bulletproof-desc = Тяжёлый бронежилет против пуль
faction-armor-riot = Противоударный костюм
faction-armor-riot-desc = Защита от ближнего боя и толпы
faction-armor-web = Разгрузочный бронежилет
faction-armor-web-desc = Бронепластины и подсумки

## (builder removed)

## Shop Messages
shop-not-enough-points = Недостаточно очков
shop-item-purchased = { $item } куплен за { $points ->
    [1] { $points } очко
    [2] { $points } очка
    [3] { $points } очка
    [4] { $points } очка
   *[other] { $points } очков
}
shop-current-points = У вас { $points ->
    [1] { $points } очко
    [2] { $points } очка
    [3] { $points } очка
    [4] { $points } очка
   *[other] { $points } очков
}
shop-faction-points = У фракции { $faction } { $points } очков
