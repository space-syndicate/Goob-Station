entity-condition-guidebook-total-damage =
    { $max ->
        [2147483648] у него как минимум {NATURALFIXED($min, 2)} общего урона
        *[other] { $min ->
                    [0] у него не более {NATURALFIXED($max, 2)} общего урона
                    *[other] у него от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} общего урона
                 }
    }

entity-condition-guidebook-type-damage =
    { $max ->
        [2147483648] у него как минимум {NATURALFIXED($min, 2)} урона {$type}
        *[other] { $min ->
                    [0] у него не более {NATURALFIXED($max, 2)} урона {$type}
                    *[other] у него от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} урона {$type}
                 }
    }

entity-condition-guidebook-group-damage =
    { $max ->
        [2147483648] у него как минимум {NATURALFIXED($min, 2)} урона {$type}.
        *[other] { $min ->
                    [0] у него не более {NATURALFIXED($max, 2)} урона {$type}.
                    *[other] у него от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} урона {$type}
                 }
    }

entity-condition-guidebook-total-hunger =
    { $max ->
        [2147483648] у цели как минимум {NATURALFIXED($min, 2)} общего голода
        *[other] { $min ->
                    [0] у цели не более {NATURALFIXED($max, 2)} общего голода
                    *[other] у цели от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} общего голода
                 }
    }

entity-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] присутствует как минимум {NATURALFIXED($min, 2)}u {$reagent}
        *[other] { $min ->
                    [0] присутствует не более {NATURALFIXED($max, 2)}u {$reagent}
                    *[other] присутствует от {NATURALFIXED($min, 2)}u до {NATURALFIXED($max, 2)}u {$reagent}
                 }
    }

entity-condition-guidebook-mob-state-condition =
    сущность находится в состоянии { $state }

entity-condition-guidebook-job-condition =
    работа цели — { $job }

entity-condition-guidebook-solution-temperature =
    температура раствора { $max ->
            [2147483648] как минимум {NATURALFIXED($min, 2)} K
            *[other] { $min ->
                        [0] не более {NATURALFIXED($max, 2)} K
                        *[other] от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} K
                     }
    }

entity-condition-guidebook-body-temperature =
    температура тела { $max ->
            [2147483648] как минимум {NATURALFIXED($min, 2)} K
            *[other] { $min ->
                        [0] не более {NATURALFIXED($max, 2)} K
                        *[other] от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} K
                     }
    }

entity-condition-guidebook-organ-type =
    метаболизирующий орган { $shouldhave ->
                                [true] является
                                *[false] не является
                           } органом {$name}

entity-condition-guidebook-has-tag =
    цель { $invert ->
                 [true] не имеет
                 *[false] имеет
                } тег {$tag}

entity-condition-guidebook-this-reagent = этот реагент

entity-condition-guidebook-breathing =
    метаболизатор { $isBreathing ->
                [true] нормально дышит
                *[false] задыхается
               }

entity-condition-guidebook-internals =
    метаболизатор { $usingInternals ->
                [true] использует внутреннее дыхание
                *[false] дышит атмосферным воздухом
               }
