ent-WallMedkit = настенная аптечка
    .desc = Вкалывает дозу сыворотки и залатывает лёгкие раны тому, кто ей воспользуется.
ent-WallMedkitEmpty = { ent-WallMedkit }
    .desc = { ent-WallMedkit.desc }
    .suffix = Пустая
ent-WallMedkitRefill = набор пополнения настенной аптечки
    .desc = Запечатанный комплект картриджей. Используйте на настенной аптечке, чтобы восстановить заряды.
ent-CrateWallMedkitRefill = ящик наборов пополнения настенных аптечек
    .desc = Содержит наборы пополнения для настенных аптечек.

wall-medkit-examine = Осталось доз: [color=lime]{ $charges }[/color] из { $max }.
wall-medkit-empty = Аптечка пуста. Нужен набор пополнения.
wall-medkit-no-bloodstream = Аптечке некуда сделать укол.
wall-medkit-bloodstream-full = Ваш организм не может принять ещё одну дозу.
wall-medkit-injected-self = Игла входит в руку — вам введена сыворотка, стало немного легче. Осталось доз: { $charges }/{ $max }.
wall-medkit-injected-others = { CAPITALIZE(THE($user)) } пользуется настенной аптечкой.
wall-medkit-refill-full = Аптечка уже полностью заряжена.
wall-medkit-refilled = Аптечка пополнена. Доз: { $charges }/{ $max }.
