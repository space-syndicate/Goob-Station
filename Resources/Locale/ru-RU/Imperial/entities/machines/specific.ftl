ent-MachineMaterialSilo = силос для материалов
    .desc = Продвинутая машина, способная использовать блюспейс технологию для передачи материалов подключенным машинам.
ent-MaterialSiloMachineCircuitboard = силос для материалов (машинная плата)
    .desc = Машинная плата требуемая для постройки силоса для материалов.

ent-WeaponEnergyTurretStationBase = охранная турель
    .desc = Высокотехнологичная автономная система оружия, предназначенная для предотвращения доступа посторонних лиц в охраняемые зоны.
ent-WeaponEnergyTurretAI = охранная турель ИИ
    .desc = { ent-WeaponEnergyTurretStationBase.desc }
ent-WeaponEnergyTurretSecurity = охранная турель СБ
    .desc = { ent-WeaponEnergyTurretStationBase.desc }
ent-WeaponEnergyTurretCommand = охранная турель командования
    .desc = { ent-WeaponEnergyTurretStationBase.desc }

ent-WeaponEnergyTurretStationMachineCircuitboardBase = охранная турель (машинная плата)
    .desc = Машинная плата для охранной турели.
ent-WeaponEnergyTurretAIMachineCircuitboard = охранная турель ИИ (машинная плата)
    .desc = { ent-WeaponEnergyTurretStationMachineCircuitboardBase.desc }
ent-WeaponEnergyTurretSecurityMachineCircuitboard = охранная турель СБ (машинная плата)
    .desc = { ent-WeaponEnergyTurretStationMachineCircuitboardBase.desc }

ent-WeaponEnergyTurretStationControlPanelBase = панель управления турелями
    .desc = Настенный интерфейс для удаленной настройки рабочих параметров связанных охранных турелей.
ent-WeaponEnergyTurretAIControlPanel = панель управления турелями ИИ
    .desc = { ent-WeaponEnergyTurretStationControlPanelBase.desc }
ent-WeaponEnergyTurretSecurityControlPanel = панель управления турелями СБ
    .desc = { ent-WeaponEnergyTurretStationControlPanelBase.desc }
ent-WeaponEnergyTurretCommandControlPanel = панель управления турелями командования
    .desc = { ent-WeaponEnergyTurretStationControlPanelBase.desc }

ent-BulletEnergyTurretBase = энергетический снаряд
ent-BulletEnergyTurretLaser = лазерный снаряд
ent-BulletEnergyTurretDisabler = стан-снаряд

# панель управления

# Заголовки
turret-controls-window-title = Автономная Система Управления Защитой
turret-controls-window-turret-status-label = Соединённых устройств: [{$count}]
turret-controls-window-armament-controls-label = Режим Защиты
turret-controls-window-targeting-controls-label = Авторизованный персонал

# Статус
turret-controls-window-no-turrets = <! Нет соединённых устройств !>
turret-controls-window-turret-status = » {$device} - Статус: {$status}
turret-controls-window-turret-disabled = ***ОФФЛАЙН***
turret-controls-window-turret-retracted = НЕАКТИВНА
turret-controls-window-turret-retracting = ДЕАКТИВАЦИЯ
turret-controls-window-turret-deployed = АКТИВНА
turret-controls-window-turret-deploying = АКТИВАЦИЯ
turret-controls-window-turret-firing = ЦЕЛЬ ОБНАРУЖЕНА
turret-controls-window-turret-error = ОШИБКА
turret-controls-window-turret-broken = ***НЕИСПРАВНА***

# Кнопки
turret-controls-window-safe = Отключение
turret-controls-window-stun = Стан
turret-controls-window-lethal = Летальный
turret-controls-window-ignore = Игнорирование
turret-controls-window-target = Цель
turret-controls-window-access-group-label = {$prefix} {$label}
turret-controls-window-all-checkbox = Все

# Текст
turret-controls-window-footer = Неавторизованный персонал должен убедиться, что средства защиты отключены.

# Предупреждения
turret-controls-access-denied = В доступе отказано.
