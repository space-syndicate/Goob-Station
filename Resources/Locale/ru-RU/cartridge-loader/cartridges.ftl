default-program-namlot-name-cartridge = Картридж

default-program-name = Программа
notekeeper-program-name = Заметки
nano-task-program-name = НаноЗадачи
news-read-program-name = Новости станции

crew-manifest-program-name = Манифест экипажа
crew-manifest-cartridge-loading = Загрузка...

net-probe-program-name = NetProbe
net-probe-scan = Просканировать {$device}!
net-probe-label-name = Название
net-probe-label-address = Адрес
net-probe-label-frequency = Частота
net-probe-label-network = Сеть

log-probe-program-name = LogProbe
log-probe-scan = Журналы загружены с {$device}!
log-probe-label-time = Время
log-probe-label-accessor = Доступен
log-probe-label-number = #
log-probe-print-button = Распечатать
log-probe-printout-device = Просканированное устройство: {$name}
log-probe-printout-header = Последние использования доступов:
log-probe-printout-entry  = #{$number} / {$time} / {$accessor}

astro-nav-program-name = АстроНав

med-tek-program-name = МедТек

# Картридж НаноЗадачи

nano-task-ui-heading-high-priority-tasks =
    { $amount ->
        [zero] Нет задач с высоким приоритетом
        [one] 1 задача с высоким приоритетом
       *[other] {$amount} высокоприоритетных задач
    }
nano-task-ui-heading-medium-priority-tasks =
    { $amount ->
        [zero] Нет задач с средним приоритетом
        [one] 1 задача с средним приоритетом
       *[other] {$amount} среднеприоритетных задач
    }
nano-task-ui-heading-low-priority-tasks =
    { $amount ->
        [zero] Нет задач с низким приоритетом
        [one] 1 задача с низким приоритетом
       *[other] {$amount} низкоприоритетных задач
    }
nano-task-ui-done = Выполнено
nano-task-ui-revert-done = Отменить
nano-task-ui-priority-low = Низкий
nano-task-ui-priority-medium = Средний
nano-task-ui-priority-high = Высокий
nano-task-ui-cancel = Закрыть
nano-task-ui-print = Распечатать
nano-task-ui-delete = Удалить
nano-task-ui-save = Сохранить
nano-task-ui-new-task = Новая задача
nano-task-ui-description-label = Описание:
nano-task-ui-description-placeholder = Сделать что-то важное
nano-task-ui-requester-label = Запросивший:
nano-task-ui-requester-placeholder = Джон Нанотрайсен
nano-task-ui-item-title = Редактировать задачу
nano-task-printed-description = [bold]Описание[/bold]: {$description}
nano-task-printed-requester = [bold]Запросивший[/bold]: {$requester}
nano-task-printed-high-priority = [bold]Приоритет[/bold]: [color=red]Высокий[/color]
nano-task-printed-medium-priority = [bold]Приоритет[/bold]: Средний
nano-task-printed-low-priority = [bold]Приоритет[/bold]: Низкий

# Список разыскиваемых
wanted-list-program-name = Список разыскиваемых
wanted-list-label-no-records = Всё в порядке, ковбой
wanted-list-search-placeholder = Поиск по имени и статусу

wanted-list-age-label = [color=darkgray]Возраст:[/color] [color=white]{$age}[/color]
wanted-list-job-label = [color=darkgray]Работа:[/color] [color=white]{$job}[/color]
wanted-list-species-label = [color=darkgray]Вид:[/color] [color=white]{$species}[/color]
wanted-list-gender-label = [color=darkgray]Пол:[/color] [color=white]{$gender}[/color]

wanted-list-reason-label = [color=darkgray]Причина:[/color] [color=white]{$reason}[/color]
wanted-list-unknown-reason-label = неизвестная причина

wanted-list-initiator-label = [color=darkgray]Инициатор:[/color] [color=white]{$initiator}[/color]
wanted-list-unknown-initiator-label = неизвестный инициатор

wanted-list-status-label = [color=darkgray]Статус:[/color] {$status ->
        [suspected] [color=yellow]подозреваемый[/color]
        [wanted] [color=red]разыскиваемый[/color]
        [detained] [color=#b18644]задержанный[/color]
        [paroled] [color=green]условно освобожденный[/color]
        [discharged] [color=green]освобожденный[/color]
        *[other] другое
    }

wanted-list-history-table-time-col = Время
wanted-list-history-table-reason-col = Преступление
wanted-list-history-table-initiator-col = Инициатор
