# Imperial Space
English text below.

Это репозиторий для приема добровольных вкладов в пользу Imperial Space. [Imperial Space](https://wiki.imperialspace.net) - это мод-глобальная конверсия на игру Space Station 14, созданную [Wizard Den](https://spacestation14.io/), и лицензированную под лицензией [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT). Обратите внимание, что эта сборка не имеет полного контента и намеренно сделана неиграбельной. Пожалуйста, не используйте эту сборку для хостинга серверов.

This repository is for accepting voluntary contributions in support of [Imperial Space](https://wiki.imperialspace.net). Imperial Space is a global conversion mod for the game Space Station 14, created by [Wizard Den](https://spacestation14.io/) and licensed under the [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT). Please note that this build does not include the full content and is intentionally unplayable. Do not use this build to host servers.

## Imperial Space License RUS

Этот репозиторий представляет собой глобальную конверсию на игру Space Station 14. Практически все изменения, внесенные в оригинальную игру, защищены лицензиями [ICLA](https://wiki.imperialspace.net/icla) и [IELA](https://wiki.imperialspace.net/iela). Поэтому использование сборки в целях хостинга серверов без письменного разрешения правообладателя запрещено и отслеживается.
В этом репозитории содержится в том числе код оригинальной игры, который защищен лицензией [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT). Помимо этого, в этом репозитории содержится контент, защищенный лицензями [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/).
Все ассеты имеют собственную лицензию, указанную в файле metadata. [Пример](https://github.com/imperial-space/SW-public/blob/develop/Resources/Textures/Imperial/Medieval/Clothing/Armor/brigantin.rsi/meta.json)

## Imperial Space License ENG

This repository is a global conversion mod for the game Space Station 14. Nearly all modifications made to the original game are protected by the [ICLA](https://wiki.imperialspace.net/icla) and [IELA](https://wiki.imperialspace.net/iela) licenses. Therefore, using this build to host servers without the copyright holder’s written permission is prohibited and monitored.
This repository also includes original game code, which is licensed under the [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT) License. In addition, it contains content licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/).
Assets have their license and the copyright in the metadata file. [Example](https://github.com/imperial-space/SW-public/blob/develop/Resources/Textures/Imperial/Medieval/Clothing/Armor/brigantin.rsi/meta.json).

## Запуск локалки
### Требуемые программы
Если вы хотите запустить именно нашу сборку, то вам понадобится данное программное обеспечение: 
1. [Git](https://git-scm.com/downloads) 
1. [DotNet SDK 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
1. [Python](https://www.python.org/downloads/)

### Пошаговая инструкция установки

#### 1. Открываем консоль
Нажать <kbd>Win</kbd> или кликнуть <kbd>ЛКМ</kbd> по строке поиска рядом с кнопкой "Пуск", ввести `cmd` или `Командная строка` и открыть найденное приложение.

#### 2. Устанавливаем сборку
Вводим одну из следующих команд в консоль — в зависимости от нужной сборки:
```
git clone https://github.com/imperial-space/SS14-public
```
Ожидаем завершения загрузки. Репозиторий появится по пути `C:\Users\(имя вашего пользователя)\SS14-public` или в той папке, где вы выполнили команду.
Путь для установки можно изменить, для этого перед установкой следует использовать `cd Диск/Путь`
#### 3. Переходим в папку сборки
Вводим следующию команду в консоль:
```
cd SS14-public
```
#### 4. Обновляем подмодули сборки
Вводим в консоль:
```
git submodule update --init --recursive
```
#### 5. Собираем сборку
Вводим в консоль:

```
dotnet build -c release
```

Использование команды `dotnet build` без аргументов запускает сервер в DEV режиме. В нём при входе вы появляетесь в роли капитана на DEV-карте с некоторыми ограничениями, а также при любой ошибке сервер будет завершать работу с фатальным логом в консоли. **Этот режим не подходит для маппинга**.

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)


## About Space Station 14

Space Station 14 is a remake of SS13 that runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), Wizard's Den homegrown engine written in C#.
[Website](https://spacestation14.io/) | [Discord](https://discord.ss14.io/) | [Forum](https://forum.spacestation14.io/) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.io/about/nightlies/)
