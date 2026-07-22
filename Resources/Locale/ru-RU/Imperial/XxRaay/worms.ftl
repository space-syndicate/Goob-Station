ent-MobWormTier1 = кровавый червь
    .desc = Маленький кроваво-красный червяк.

ent-MobWormTier2 = кровавый червь
    .desc = Толстый, жилистый червь с тяжёлой челюстью и свежими шрамами по телу.

ent-MobWormTier3 = кровавый червь
    .desc = Огромный кровавый червь в плотных пластинах.

ghost-role-information-blood-worm-name = кровавый червь
ghost-role-information-blood-worm-description = Проберитесь через вентиляцию, пейте кровь экипажа, эволюционируйте и размножайтесь.
ghost-role-information-blood-worm-rules = Вы являетесь [color={role-type-team-antagonist-color}][bold]{role-type-team-antagonist-name}[/bold][/color] вместе со всеми другими кровавыми червями.

station-event-worm-vent-spawn-start-announcement = Внимание. В вентиляционной системе станции обнаружены агрессивные биосигнатуры неизвестного происхождения. Изолируйте подозрительные участки и избегайте одиночных перемещений.

worm-blood-alert-name = Запас крови
worm-blood-alert-desc = Кровь, накопленная в теле червя. Максимум 1000 единиц.

vent-crawler-verb-enter = Залезть в вентиляцию
vent-crawler-verb-exit = Вылезти из вентиляции

vent-crawler-fail-in-vent = Вы уже в вентиляции.
vent-crawler-fail-corpse = Нельзя залезть в вентиляцию, пока вы вселены в труп.
vent-crawler-fail-door = Нельзя залезть в вентиляцию, пока вы прячетесь в шлюзе.
vent-crawler-fail-drinking = Нельзя залезть в вентиляцию, пока вы пьёте кровь.
vent-crawler-fail-evolving = Нельзя залезть в вентиляцию во время эволюции.
vent-crawler-fail-reproducing = Нельзя залезть в вентиляцию во время размножения.
vent-crawler-fail-welded = Вентиляция заварена.
vent-crawler-fail-invalid = Сюда нельзя залезть.

worm-door-hide-verb-enter = Спрятаться в шлюзе
worm-door-hide-verb-exit = Вылезти из шлюза

worm-door-hide-fail-vent = Нельзя спрятаться в шлюзе, находясь в вентиляции.
worm-door-hide-fail-possessing = Нельзя спрятаться в шлюзе, пока вы вселены в труп.
worm-door-hide-fail-drinking = Нельзя спрятаться в шлюзе, пока вы пьёте кровь.
worm-door-hide-fail-occupied = В этом шлюзе уже кто-то прячется.
worm-door-hide-fail-open = Шлюз должен быть закрыт.
worm-door-hide-fail-welded = Нельзя спрятаться в заваренном шлюзе.
worm-door-hide-fail-armored = Нельзя спрятаться в бронированном шлюзе.
worm-door-hide-fail-hiding = Нельзя делать это, пока вы прячетесь в шлюзе.
worm-door-hide-fail-evolving = Нельзя спрятаться в шлюзе во время эволюции.
worm-door-hide-fail-reproducing = Нельзя спрятаться в шлюзе во время размножения.
worm-door-hide-ambush = Червь выпрыгивает из шлюза и валит вас с ног!

ent-ActionWormBloodDrink = Пить кровь
    .desc = Присосаться к существу и высасывать кровь.

worm-blood-drink-attach = Присасывается к {$target}!

worm-blood-drink-fail-vent = Нельзя пить кровь, находясь в вентиляции.
worm-blood-drink-fail-door = Нельзя пить кровь, пока вы прячетесь в шлюзе.
worm-blood-drink-fail-corpse = Нельзя пить кровь, пока вы вселены в труп.
worm-blood-drink-fail-evolving = Нельзя пить кровь во время эволюции.
worm-blood-drink-fail-reproducing = Нельзя пить кровь во время размножения.
worm-blood-drink-fail-no-blood = У цели нет крови.
worm-blood-drink-fail-low-blood = У цели слишком мало крови.

ent-ActionWormCorpseEnter = Залезть в труп
    .desc = Вселиться в мёртвое тело и управлять им.

ent-ActionWormCorpseExit = Покинуть труп
    .desc = Вылезти из носителя, нанеся ему сильное кровотечение.

worm-corpse-fail-vent = Нельзя вселиться, находясь в вентиляции.
worm-corpse-fail-door = Нельзя вселиться, пока вы прячетесь в шлюзе.
worm-corpse-fail-drinking = Нельзя вселиться, пока вы пьёте кровь.
worm-corpse-fail-evolving = Нельзя вселиться во время эволюции.
worm-corpse-fail-reproducing = Нельзя вселиться во время размножения.
worm-corpse-fail-occupied = Этот труп уже занят.
worm-corpse-fail-worm = Нельзя вселиться в другого червя.
worm-corpse-fail-invalid = Сюда нельзя вселиться.
worm-corpse-fail-not-dead = Цель должна быть мёртвой.
worm-corpse-fail-cooldown = Нужно подождать перед повторным вселением.
worm-corpse-fail-already-possessing = Вы уже вселены в труп.
worm-corpse-fail-possessing = Нельзя пить кровь, пока вы вселены в труп.

ent-ActionWormEvolution = Эволюция
    .desc = Соткать кокон и превратиться в более сильную форму, потратив накопленную кровь.

worm-evolution-weaving = Вьёт кокон

worm-evolution-fail-blood = Недостаточно крови для эволюции. (Требуется: {$cost})
worm-evolution-fail-vent = Нельзя эволюционировать, находясь в вентиляции.
worm-evolution-fail-door = Нельзя эволюционировать, пока вы прячетесь в шлюзе.
worm-evolution-fail-corpse = Нельзя эволюционировать, пока вы вселены в труп.
worm-evolution-fail-drinking = Нельзя эволюционировать, пока вы пьёте кровь.
worm-evolution-fail-evolving = Вы уже эволюционируете.
worm-evolution-fail-reproducing = Нельзя эволюционировать во время размножения.

worm-cocoon-timer-name = Эволюция
worm-cocoon-timer-desc = До завершения превращения осталось времени.

ent-ActionWormCocoonObserve = Наблюдать
    .desc = Переключиться на случайного другого червя.

worm-cocoon-observe-fail-none = Нет других червей для наблюдения.
worm-cocoon-observe-start = Вы наблюдаете за {$target}.

ent-WormCocoonTier1 = кокон червя
    .desc = Пульсирующий кокон, сотканный из крови и слизи.

ent-WormCocoonTier2 = кокон червя
    .desc = Плотный кокон с твёрдой оболочкой, внутри что-то перерождается.

ent-ActionWormReproduction = Размножение
    .desc = Соткать кокон и породить трёх маленьких червей, потратив 300 единиц крови. Вы откатитесь на первую стадию.

worm-reproduction-weaving = Вьёт кокон

worm-reproduction-fail-blood = Недостаточно крови для размножения. (Требуется: {$cost})
worm-reproduction-fail-vent = Нельзя размножаться, находясь в вентиляции.
worm-reproduction-fail-door = Нельзя размножаться, пока вы прячетесь в шлюзе.
worm-reproduction-fail-corpse = Нельзя размножаться, пока вы вселены в труп.
worm-reproduction-fail-drinking = Нельзя размножаться, пока вы пьёте кровь.
worm-reproduction-fail-reproducing = Вы уже размножаетесь.
worm-reproduction-fail-evolving = Нельзя размножаться во время эволюции.

ent-WormCocoonReproduction = кокон размножения
    .desc = Пульсирующий кокон, внутри созревают новые черви.
