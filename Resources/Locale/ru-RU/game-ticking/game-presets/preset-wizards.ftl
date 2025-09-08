## Выживший

roles-antag-survivor-name = Выживший
# Это отсылка к Halo
roles-antag-survivor-objective = Текущая цель: Выжить

survivor-role-greeting =
    Вы Выживший.
    Прежде всего, вам нужно вернуться на ЦентКом живым.
    Соберите столько оружия, сколько нужно, чтобы гарантировать ваше выживание. Вы можете убивать только других [color=red]Выживших[/color]. Таковыми считаются только те, кто имеет огнестрельное, лазерное оружие или магию. 
    Не доверяйте никому.

survivor-round-end-dead-count =
{
    $deadCount ->
        [one] [color=red]{$deadCount}[/color] выживший погиб.
        *[other] [color=red]{$deadCount}[/color] выживших погибло.
}

survivor-round-end-alive-count =
{
    $aliveCount ->
        [one] [color=yellow]{$aliveCount}[/color] выживший остался на станции.
        *[other] [color=yellow]{$aliveCount}[/color] выживших осталось на станции.
}

survivor-round-end-alive-on-shuttle-count =
{
    $aliveCount ->
        [one] [color=green]{$aliveCount}[/color] выживший выбрался живым.
        *[other] [color=green]{$aliveCount}[/color] выживших выбралось живым.
}

## Маг

objective-issuer-swf = [color=turquoise]Космическая Федерация Магов[/color]

wizard-title = Маг
wizard-description = На станции есть Маг! Никогда не знаешь, что он может сделать.

roles-antag-wizard-name = Маг
roles-antag-wizard-objective = Преподай им урок, который они никогда не забудут.

wizard-role-greeting =
    ТЫ МАГ!
    Между Космической Федерацией Магов и НаноТрейзен возникли разногласия.
    Итак, Космическая Федерация Магов выбрала тебя для посещения станции.
    Покажи им свои способности.
    Что ты будешь делать, решать тебе, просто помни, что Высшие Маги хотят, чтобы ты выбрался живым.

wizard-round-end-name = Маг

## TODO: Ученик Мага (Появится после релиза Wizard)