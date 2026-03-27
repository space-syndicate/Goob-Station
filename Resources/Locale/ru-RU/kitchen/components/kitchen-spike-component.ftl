comp-kitchen-spike-deny-collect = На {CAPITALIZE($this)} уже что-то есть, закончите сначала собирать мясо!
comp-kitchen-spike-deny-butcher = {CAPITALIZE($victim)} не может быть разделан на {$this}.
comp-kitchen-spike-deny-butcher-knife = {CAPITALIZE($victim)} не может быть разделан на {$this}, вам нужно разделать его с помощью ножа.
comp-kitchen-spike-deny-not-dead = {CAPITALIZE($victim)} не может быть разделан. {CAPITALIZE(SUBJECT($victim))} {CONJUGATE-BE($victim)} не умер!

comp-kitchen-spike-begin-hook-victim = {$user} начинает насаживать вас на {$this}!
comp-kitchen-spike-begin-hook-self = Вы начинаете насаживать себя на {$this}!

comp-kitchen-spike-kill = { CAPITALIZE(THE($user)) } насадил { THE($victim) } на { THE($this) }, мгновенно убив { OBJECT($victim) }!

comp-kitchen-spike-suicide-other = { CAPITALIZE(THE($victim)) } насадил { REFLEXIVE($victim) } на { THE($this) }!
comp-kitchen-spike-suicide-self = Вы насаживаете себя на { THE($this) }!
comp-kitchen-spike-begin-hook-self-other = { CAPITALIZE(THE($victim)) } начинает насаживать { REFLEXIVE($victim) } на { THE($hook) }!

comp-kitchen-spike-knife-needed = Вам нужен нож для этого.
comp-kitchen-spike-remove-meat = Вы срезаете немного мяса с {$victim}.
comp-kitchen-spike-remove-meat-last = Вы срезаете последний кусок мяса с {$victim}!

comp-kitchen-spike-meat-name = { $name } ({ $victim })

comp-kitchen-spike-begin-hook-other-self = Вы начинаете насаживать { CAPITALIZE(THE($victim)) } на { THE($hook) }!
comp-kitchen-spike-begin-hook-other = { CAPITALIZE(THE($user)) } начинает насаживать { CAPITALIZE(THE($victim)) } на { THE($hook) }!

comp-kitchen-spike-hook-self = Вы начинаете насаживать себя на { THE($hook) }!
comp-kitchen-spike-hook-self-other = { CAPITALIZE(THE($victim)) } насаживает { REFLEXIVE($victim) } на { THE($hook) }!

comp-kitchen-spike-hook-other-self = Вы насаживаете { CAPITALIZE(THE($victim)) } на { THE($hook) }!
comp-kitchen-spike-hook-other = { CAPITALIZE(THE($user)) } насаживает { CAPITALIZE(THE($victim)) } на { THE($hook) }!

comp-kitchen-spike-begin-unhook-self = Вы снимаете себя с { THE($hook) }!
comp-kitchen-spike-begin-unhook-self-other = { CAPITALIZE(THE($victim)) } начал снимать { REFLEXIVE($victim) } с { THE($hook) }!

comp-kitchen-spike-begin-unhook-other-self = Вы начали снимать { CAPITALIZE(THE($victim)) } с { THE($hook) }!
comp-kitchen-spike-begin-unhook-other = { CAPITALIZE(THE($user)) } начал снимать { CAPITALIZE(THE($victim)) } с { THE($hook) }!

comp-kitchen-spike-unhook-self = Вы сняли себя с { THE($hook) }!
comp-kitchen-spike-unhook-self-other = { CAPITALIZE(THE($victim)) } снял { REFLEXIVE($victim) } с { THE($hook) }!

comp-kitchen-spike-unhook-other-self = Вы сняли { CAPITALIZE(THE($victim)) } с { THE($hook) }!
comp-kitchen-spike-unhook-other = { CAPITALIZE(THE($user)) } снял { CAPITALIZE(THE($victim)) } с { THE($hook) }!

comp-kitchen-spike-begin-butcher-self = Вы начали разделывать { THE($victim) }!
comp-kitchen-spike-begin-butcher = { CAPITALIZE(THE($user)) } начал разделывать { THE($victim) }!

comp-kitchen-spike-butcher-self = Вы разделали { THE($victim) }!
comp-kitchen-spike-butcher = { CAPITALIZE(THE($user)) } разделал { THE($victim) }!

comp-kitchen-spike-unhook-verb = Снять с крюка

comp-kitchen-spike-hooked = [color=red]{ CAPITALIZE(THE($victim)) } на крюке![/color]

comp-kitchen-spike-victim-examine = [color=orange]{ CAPITALIZE(SUBJECT($target)) } выглядит довольно худым.[/color]
