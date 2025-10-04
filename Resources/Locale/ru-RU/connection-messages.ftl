whitelist-not-whitelisted = Вас нет в вайтлисте.

# proper handling for having a min/max or not
whitelist-playercount-invalid = {$min ->
    [0] Вайтлист для этого сервера применяется только для числа игроков ниже {$max}.
    *[other] Вайтлист для этого сервера применяется только для числа игроков выше {$min} {$max ->
        [2147483647] -> так что, возможно, вы сможете присоединиться позже.
       *[other] ->  и ниже {$max} игроков, так что, возможно, вы сможете присоединиться позже.
    }
}
whitelist-not-whitelisted-rp = Вас нет в вайтлисте. Чтобы попасть в вайтлист, посетите наш Discord (ссылку можно найти по адресу: нету).

cmd-whitelistadd-desc = Добавить игрока с указанным юзернеймом в вайтлист.
cmd-whitelistadd-help = whitelistadd <username>
cmd-whitelistadd-existing = {$username} уже в вайтлисте!
cmd-whitelistadd-added = {$username} добавлен в вайтлист
cmd-whitelistadd-not-found = Пользователь '{$username}' не найден
cmd-whitelistadd-arg-player = [player]

cmd-whitelistremove-desc = Удалить игрока с указанным юзернеймом из вайтлиста.
cmd-whitelistremove-help = whitelistremove <username>
cmd-whitelistremove-existing = {$username} не в вайтлисте!
cmd-whitelistremove-removed = Пользователь {$username} удалён из вайтлиста
cmd-whitelistremove-not-found = Пользователь '{$username}' не найден
cmd-whitelistremove-arg-player = [player]

cmd-kicknonwhitelisted-desc = Кикнуть с сервера всех пользователей не из вайтлиста.
cmd-kicknonwhitelisted-help = kicknonwhitelisted

ban-banned-permanent = Вы получили перманентный бан.
ban-banned-permanent-appeal = Вы получили перманентный бан.
ban-expires = Вы получили бан на {$duration} минут, и он истечёт {$time} по UTC (для московского времени добавьте 3 часа).
ban-banned-1 = Вам, или другому пользователю этого компьютера или соединения, запрещено здесь играть.
ban-banned-2 = Причина бана: "{$reason}"
ban-banned-3 = Попытки обойти этот бан, например, путём создания нового аккаунта, будут фиксироваться.

soft-player-cap-full = Сервер заполнен!
panic-bunker-account-denied = Этот сервер находится в режиме "Бункер". В данный момент новые подключения не принимаются. Повторите попытку позже
panic-bunker-account-denied-reason = Этот сервер находится в режиме "Бункер", и вам было отказано в доступе. Причина: "{$reason}"
panic-bunker-account-reason-account = Ваш аккаунт должен быть старше {$minutes} минут
panic-bunker-account-reason-overall = Необходимо минимальное отыгранное время {$hours} часов

ban-you-can-appeal = Вы можете обжаловать бан, для этого откройте соотвествующий тикет в канале "поддержка" в нашем Discord

whitelist-playtime = У вас недостаточно игрового времени, чтобы присоединиться к этому серверу. Чтобы присоединиться к этому серверу, вам потребуется как минимум {$minutes} минут игрового времени.
whitelist-player-count = В данный момент этот сервер не принимает игроков. Пожалуйста, повторите попытку позже.
whitelist-notes = Введите /adminremarks в чате. На данный момент у вас слишком много заметок администратора, чтобы присоединиться к этому серверу. Вы можете проверить свои заметки, введя /adminremarks в чате.
whitelist-manual = Вы не внесены в белый список на этом сервере.
whitelist-fail-prefix = Не внесен в белый список: {$msg}
whitelist-blacklisted = Вы занесены в черный список на этом сервере.
whitelist-always-deny = Вам запрещено подключаться к этому серверу.

baby-jail-account-denied = Этот сервер предназначен для новичков и тех, кто хочет им помочь. Новые подключения от учетных записей, которые слишком старые или не находятся в белом списке, не принимаются. Попробуйте другие серверы и узнайте, что еще может предложить Space Station 14. Удачи!
baby-jail-account-denied-reason = Этот сервер предназначен для новичков и тех, кто хочет им помочь. Новые подключения от учетных записей, которые слишком старые или не находятся в белом списке, не принимаются. Попробуйте другие серверы и узнайте, что еще может предложить Space Station 14. Удачи! Причина: "{ $reason }"
baby-jail-account-reason-account = Ваша учетная запись Space Station 14 слишком старая. Она должна быть моложе { $minutes } минут.
baby-jail-account-reason-overall = Ваше общее время игры на сервере должно быть меньше { $minutes } минут.
cmd-blacklistadd-desc = Добавляет игрока с указанным именем пользователя в чёрный список сервера.
cmd-blacklistadd-help = Использование: blacklistadd <username>
cmd-blacklistadd-existing = { $username } уже в чёрном списке!
cmd-blacklistadd-added = { $username } добавлен в чёрный список
cmd-blacklistadd-not-found = Не удалось найти '{ $username }'
cmd-blacklistadd-arg-player = [player]
cmd-blacklistremove-desc = Удаляет игрока с указанным именем пользователя из чёрного списка сервера.
cmd-blacklistremove-help = Использование: blacklistremove <имя пользователя>
cmd-blacklistremove-existing = { $username } не в чёрном списке!
cmd-blacklistremove-removed = { $username } удалён из чёрного списка
cmd-blacklistremove-not-found = Не удалось найти '{ $username }'
cmd-blacklistremove-arg-player = [player]

generic-misconfigured = Сервер неправильно настроен и не принимает игроков. Пожалуйста, свяжитесь с владельцем сервера и повторите попытку позже.
ipintel-server-ratelimited = На этом сервере используется система безопасности с внешней проверкой, которая достигла своего максимального предела проверки. Пожалуйста, обратитесь за помощью к администрации сервера и повторите попытку позже.
ipintel-unknown = На этом сервере используется система безопасности с внешней проверкой, но она столкнулась с ошибкой. Пожалуйста, обратитесь за помощью к администрации сервера и повторите попытку позже.
ipintel-suspicious = Похоже, вы подключаетесь через центр обработки данных или VPN. По административным причинам мы не разрешаем играть через VPN-соединения. Пожалуйста, обратитесь за помощью к администрации сервера, если вы считаете, что это ошибочно.
