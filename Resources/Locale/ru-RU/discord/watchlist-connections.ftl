discord-watchlist-connection-header =
    { $players ->
    [one] { $players } игрок из списка наблюдения
    [few] { $players } игрока из списка наблюдения
    *[other] { $players } игроков из списка наблюдения
        } подключился{ $players ->
    [one] {""}
    [few] {"и"}
    *[other] {"и"}
        } к серверу {$serverName}

discord-watchlist-connection-entry = - {$playerName} с сообщением "{$message}"{ $expiry ->
[0] {""}
*[other] {" "}(истекает <t:{$expiry}:R>)
    }{ $otherWatchlists ->
[0] {""}
[one] {" "}и { $otherWatchlists } другим списком наблюдения
[few] {" "}и { $otherWatchlists } другими списками наблюдения
*[other] {" "}и { $otherWatchlists } другими списками наблюдения
    }
