## UI

injector-volume-transfer-label = Объём: [color=white]{$currentVolume}/{$totalVolume}u[/color]
    Режим: [color=white]{$modeString}[/color] ([color=white]{$transferVolume}u[/color])
injector-volume-label = Объём: [color=white]{$currentVolume}/{$totalVolume}u[/color]
    Режим: [color=white]{$modeString}[/color]
injector-toggle-verb-text = Переключить режим инжектора

## Entity

injector-component-inject-mode-name = впрыск
injector-component-draw-mode-name = забор
injector-component-dynamic-mode-name = динамический
injector-component-mode-changed-text = Теперь {$mode}
injector-component-transfer-success-message = Вы переливаете {$amount}u в {THE($target)}.
injector-component-transfer-success-message-self = Вы переливаете {$amount}u в себя.
injector-component-inject-success-message = Вы впрыскиваете {$amount}u в {THE($target)}!
injector-component-inject-success-message-self = Вы впрыскиваете {$amount}u в себя!
injector-component-draw-success-message = Вы забираете {$amount}u из {THE($target)}.
injector-component-draw-success-message-self = Вы забираете {$amount}u из себя.

## Fail Messages

injector-component-target-already-full-message = {CAPITALIZE(THE($target))} уже полон!
injector-component-target-already-full-message-self = Вы уже полны!
injector-component-target-is-empty-message = {CAPITALIZE(THE($target))} пуст!
injector-component-target-is-empty-message-self = Вы пусты!
injector-component-cannot-toggle-draw-message = Слишком много жидкости, чтобы забрать!
injector-component-cannot-toggle-inject-message = Нечего впрыскивать!
injector-component-cannot-toggle-dynamic-message = Невозможно переключить динамический режим!
injector-component-empty-message = {CAPITALIZE(THE($injector))} пуст!
injector-component-blocked-user = Защитное снаряжение заблокировало вашу инъекцию!
injector-component-blocked-other = {CAPITALIZE(THE(POSS-ADJ($target)))} броня заблокировала инъекцию {THE($user)}!
injector-component-cannot-transfer-message = Вы не можете перелить жидкость в {THE($target)}!
injector-component-cannot-transfer-message-self = Вы не можете перелить жидкость в себя!
injector-component-cannot-inject-message = Вы не можете сделать инъекцию в {THE($target)}!
injector-component-cannot-inject-message-self = Вы не можете сделать инъекцию в себя!
injector-component-cannot-draw-message = Вы не можете набирать шприц из {THE($target)}!
injector-component-cannot-draw-message-self = Вы не можете набирать шприц из себя!
injector-component-ignore-mobs = Этот инжектор может взаимодействовать только с контейнерами!

## mob-inject doafter messages

injector-component-needle-injecting-user = Вы начинаете вводить иглу.
injector-component-needle-injecting-target = {CAPITALIZE(THE($user))} пытается ввести вам иглу!
injector-component-needle-drawing-user = Вы начинаете набирать шприц.
injector-component-needle-drawing-target = {CAPITALIZE(THE($user))} начинает набирать шприц из вас!
injector-component-spray-injecting-user = Вы начинаете готовить распылительную насадку.
injector-component-spray-injecting-target = {CAPITALIZE(THE($user))} пытается надеть на вас распылительную насадку!

## Target Popup Success messages
injector-component-feel-prick-message = Вы чувствуете крошечный укол!
