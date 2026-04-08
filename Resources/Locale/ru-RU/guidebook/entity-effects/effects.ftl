-create-3rd-person =
    { $chance ->
        [1] Создаёт
        *[other] создают
    }

-cause-3rd-person =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    }

-satiate-3rd-person =
    { $chance ->
        [1] Утоляет
        *[other] утоляют
    }

entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Создаёт
        *[other] создают
    } { $amount ->
        [1] {INDEFINITE($entname)}
        *[other] {$amount} {MAKEPLURAL($entname)}
    }

entity-effect-guidebook-destroy =
    { $chance ->
        [1] Уничтожает
        *[other] уничтожают
    } объект

entity-effect-guidebook-break =
    { $chance ->
        [1] Ломает
        *[other] ломают
    } объект

entity-effect-guidebook-explosion =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } взрыв

entity-effect-guidebook-emp =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } электромагнитный импульс

entity-effect-guidebook-flash =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } ослепляющую вспышку

entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Создаёт
        *[other] создают
    } большое количество пены

entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Создаёт
        *[other] создают
    } большое количество дыма

entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Утоляет
        *[other] утоляют
    } { $relative ->
        [1] жажду в обычной степени
        *[other] жажду в {NATURALFIXED($relative, 3)} раз быстрее обычного
    }

entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Утоляет
        *[other] утоляют
    } { $relative ->
        [1] голод в обычной степени
        *[other] голод в {NATURALFIXED($relative, 3)} раз быстрее обычного
    }

entity-effect-guidebook-health-change =
    { $chance ->
        [1] { $healsordeals ->
                [heals] Лечит
                [deals] Наносит
                *[both] Изменяет здоровье на
             }
        *[other] { $healsordeals ->
                    [heals] лечат
                    [deals] наносят
                    *[both] изменяют здоровье на
                 }
    } { $changes }

entity-effect-guidebook-even-health-change =
    { $chance ->
        [1] { $healsordeals ->
            [heals] Равномерно лечит
            [deals] Равномерно наносит
            *[both] Равномерно изменяет здоровье на
        }
        *[other] { $healsordeals ->
            [heals] равномерно лечат
            [deals] равномерно наносят
            *[both] равномерно изменяют здоровье на
        }
    } { $changes }

entity-effect-guidebook-status-effect-old =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                     *[other] вызывают
                 } {LOC($key)} как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } {LOC($key)} как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} с накоплением
        [set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } {LOC($key)} на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        *[remove]{ $chance ->
                    [1] Удаляет
                    *[other] удаляют
                } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} эффекта {LOC($key)}
    }

entity-effect-guidebook-status-effect =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                    *[other] вызывают
                 } {LOC($key)} как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } {LOC($key)} как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} с накоплением
        [set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } {LOC($key)} как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        *[remove]{ $chance ->
                    [1] Удаляет
                    *[other] удаляют
                } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} эффекта {LOC($key)}
    } { $delay ->
        [0] немедленно
        *[other] после задержки в {NATURALFIXED($delay, 3)} секунд
    }

entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                    *[other] вызывают
                 } постоянный эффект {LOC($key)}
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } постоянный эффект {LOC($key)}
        [set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } постоянный эффект {LOC($key)}
        *[remove]{ $chance ->
                    [1] Удаляет
                    *[other] удаляют
                } {LOC($key)}
    } { $delay ->
        [0] немедленно
        *[other] после задержки в {NATURALFIXED($delay, 3)} секунд
    }

entity-effect-guidebook-knockdown =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                    *[other] вызывают
                    } {LOC($key)} как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } сбивание с ног как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} с накоплением
        *[set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } сбивание с ног как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [remove]{ $chance ->
                    [1] Удаляет
                    *[other] удаляют
                } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} эффекта сбивания с ног
    }

entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Устанавливает
        *[other] устанавливают
    } температуру раствора ровно на {NATURALFIXED($temperature, 2)} K

entity-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Отнимает
            }
        *[other]
            { $deltasign ->
                [1] добавляют
                *[-1] отнимают
            }
    } тепло у раствора, пока температура не достигнет { $deltasign ->
                [1] не более {NATURALFIXED($maxtemp, 2)} K
                *[-1] не менее {NATURALFIXED($mintemp, 2)} K
            }

entity-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Удаляет
            }
        *[other]
            { $deltasign ->
                [1] добавляют
                *[-1] удаляют
            }
    } {NATURALFIXED($amount, 2)}u реагента {$reagent} { $deltasign ->
        [1] в
        *[-1] из
    } раствора

entity-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Удаляет
            }
        *[other]
            { $deltasign ->
                [1] добавляют
                *[-1] удаляют
            }
    } {NATURALFIXED($amount, 2)}u реагентов из группы {$group} { $deltasign ->
            [1] в
            *[-1] из
        } раствора

entity-effect-guidebook-adjust-temperature =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Отнимает
            }
        *[other]
            { $deltasign ->
                [1] добавляют
                *[-1] отнимают
            }
    } {POWERJOULES($amount)} тепла { $deltasign ->
            [1] телу, в котором находится
            *[-1] от тела, в котором находится
        }

entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } болезнь { $disease }

entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } болезни { $diseases }

entity-effect-guidebook-jittering =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } дрожь

entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Очищает
        *[other] очищают
    } кровоток от других химикатов

entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Излечивает
        *[other] излечивают
    } болезни

entity-effect-guidebook-eye-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Наносит
                *[-1] Лечит
            }
        *[other]
            { $deltasign ->
                [1] наносят
                *[-1] лечат
            }
    } урон глазам

entity-effect-guidebook-vomit =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } рвоту

entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Создаёт
        *[other] создают
    } { $moles } { $moles ->
        [1] моль
        *[other] молей
    } газа { $gas }

entity-effect-guidebook-drunk =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } опьянение

entity-effect-guidebook-electrocute =
    { $chance ->
        [1] { $stuns ->
            [true] Бьёт током с оглушением
            *[false] Бьёт током
            }
        *[other] { $stuns ->
            [true] бьют током с оглушением
            *[false] бьют током
            }
    } метаболизатора на {NATURALFIXED($time, 3)} {MANY("секунд", $time)}

entity-effect-guidebook-emote =
    { $chance ->
        [1] Заставит
        *[other] заставляют
    } метаболизатора [bold][color=white]{$emote}[/color][/bold]

entity-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Тушит
        *[other] тушат
    } огонь

entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Увеличивает
        *[other] увеличивают
    } воспламеняемость

entity-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
        *[other] поджигают
    } метаболизатора

entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] Делает
        *[other] делают
    } метаболизатора разумным

entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Превращает
        *[other] превращают
    } метаболизатора в { $entityname }

entity-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1] { $deltasign ->
                [1] Вызывает
                *[-1] Уменьшает
            }
        *[other] { $deltasign ->
                    [1] вызывают
                    *[-1] уменьшают
                 }
    } кровотечение

entity-effect-guidebook-modify-blood-level =
    { $chance ->
        [1] { $deltasign ->
                [1] Увеличивает
                *[-1] Уменьшает
            }
        *[other] { $deltasign ->
                    [1] увеличивают
                    *[-1] уменьшают
                 }
    } уровень крови

entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
        *[other] парализуют
    } метаболизатора как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)}

entity-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Изменяет
        *[other] изменяют
    } скорость движения в {NATURALFIXED($sprintspeed, 3)}x как минимум на {NATURALFIXED($time, 3)} {MANY("секунд", $time)}

entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Временно отодвигает
        *[other] временно отодвигают
    } приступ нарколепсии

entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает
        *[other] смывают
    } взбитые сливки с лица

entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Излечивает
        *[other] излечивают
    } текущую зомби-инфекцию

entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Дарует
        *[other] даруют
    } индивиду зомби-инфекцию

entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Излечивает
        *[other] излечивают
    } текущую зомби-инфекцию и дарует иммунитет к будущим заражениям

entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Восстанавливает
        *[other] восстанавливают
    } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} гниения

entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } реакцию дыма или пены на {NATURALFIXED($duration, 3)} {MANY("секунд", $duration)}

entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Вызывает
        *[other] вызывают
    } добавление {$reagent} во внутренний контейнер раствора

entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Помогает
        *[other] помогают
    } разблокировать инопланетный артефакт.

entity-effect-guidebook-artifact-durability-restore =
    Восстанавливает {$restored} прочности активных узлов инопланетного артефакта.

entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Изменяет
        *[other] изменяют
    } {$attribute} на {$positive ->
    [true] [color=red]{$amount}[/color]
    *[false] [color=green]{$amount}[/color]
    }

entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает
        *[other] омолаживают
    } растение в зависимости от его возраста и времени до созревания

entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Восстанавливает
        *[other] восстанавливают
    } жизнеспособность растения, ставшего нежизнеспособным из-за мутации

entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Увеличивает
        *[other] увеличивают
    } продолжительность жизни и/или базовое здоровье растения с шансом 10% на каждый параметр

entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Увеличивает
        *[other] увеличивают
    } потенцию растения на {$increase} вплоть до максимума {$limit}. Заставляет растение терять семена, когда потенция достигает {$seedlesstreshold}. Попытка добавить потенцию сверх {$limit} может с шансом 10% снизить урожайность

entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает
        *[other] восстанавливают
    } семена растения

entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Удаляет
        *[other] удаляют
    } семена растения

entity-effect-guidebook-plant-mutate-chemicals =
    { $chance ->
        [1] Мутирует
        *[other] мутируют
    } растение так, чтобы оно производило {$name}
