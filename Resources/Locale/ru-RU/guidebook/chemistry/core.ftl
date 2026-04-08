guidebook-reagent-effect-description =
    {$quantity ->
        [0] {""}
        *[other] Если присутствует как минимум {$quantity}u {$reagent},{" "}
    }{$chance ->
        [1] { $effect }
        *[other] С вероятностью { NATURALPERCENT($chance, 2) } { $effect }
    }{ $conditionCount ->
        [0] .
        *[other] {" "}когда { $conditions }.
    }

guidebook-reagent-name = [bold][color={$color}]{CAPITALIZE($name)}[/color][/bold]
guidebook-reagent-recipes-header = Рецепт
guidebook-reagent-recipes-reagent-display = [bold]{$reagent}[/bold] \[{$ratio}\]
guidebook-reagent-sources-header = Источники
guidebook-reagent-sources-ent-wrapper = [bold]{$name}[/bold] \[1\]
guidebook-reagent-sources-gas-wrapper = [bold]{$name} (газ)[/bold] \[1\]
guidebook-reagent-recipes-mix = Смешайте
guidebook-reagent-recipes-mix-and-heat = Смешать как указано выше {$temperature}K
guidebook-reagent-effects-header = Эффекты
guidebook-reagent-effects-metabolism-stage-rate = [bold]{$stage}[/bold] [color=gray]({$rate}u в секунду)[/color]
guidebook-reagent-effects-metabolite-item = {$reagent} со скоростью { NATURALPERCENT($rate, 2) }
guidebook-reagent-effects-metabolites = Метаболизируется в { $items }.
guidebook-reagent-plant-metabolisms-header = Метаболизм растений
guidebook-reagent-plant-metabolisms-rate = [bold]Метаболизм растений[/bold] [color=gray](1u каждые 3 секунды как база)[/color]
guidebook-reagent-physical-description = [italic]Кажется, {$description}.[/italic]
guidebook-reagent-recipes-mix-info = {$minTemp ->
    [0] {$hasMax ->
            [true] {CAPITALIZE($verb)} ниже {NATURALFIXED($maxTemp, 2)}K
            *[false] {CAPITALIZE($verb)}
        }
    *[other] {CAPITALIZE($verb)} {$hasMax ->
            [true] между {NATURALFIXED($minTemp, 2)}K и {NATURALFIXED($maxTemp, 2)}K
            *[false] выше {NATURALFIXED($minTemp, 2)}K
        }
}
