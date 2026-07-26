using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Imperial.DeimonFly.BuckshotRoulette;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Управляет стрельбой, состоянием ствола, загрузкой кейсов и одноразовыми предметами Buckshot Roulette.
/// </summary>
public sealed class BuckshotRouletteSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckshotRouletteAmmoCaseComponent, MapInitEvent>(OnAmmoCaseMapInit);
        SubscribeLocalEvent<BuckshotRouletteAmmoCaseComponent, AfterInteractEvent>(OnAmmoCaseAfterInteract);
        SubscribeLocalEvent<BuckshotRouletteItemCaseComponent, MapInitEvent>(OnItemCaseMapInit);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, ExaminedEvent>(OnShotgunExamined);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, GetVerbsEvent<AlternativeVerb>>(OnShotgunGetVerbs);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, GunShotEvent>(OnShotgunFired);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, AmmoShotEvent>(OnShotgunAmmoShot);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, DroppedEvent>(OnShotgunDropped);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, UseInHandEvent>(OnShotgunUseInHand,
            before: [typeof(SharedWieldableSystem), typeof(SharedGunSystem)]);
        SubscribeLocalEvent<BuckshotRouletteBeerComponent, IngestedEvent>(OnBeerIngested);
        SubscribeLocalEvent<BuckshotRouletteToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<BuckshotRouletteToolComponent, UseInHandEvent>(OnToolUseInHand,
            before: [typeof(SharedWieldableSystem), typeof(SharedGunSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BuckshotRouletteShotgunComponent>();
        while (query.MoveNext(out var uid, out var shotgun))
        {
            if (shotgun.BarrelVisualState != BuckshotRouletteBarrelVisualState.Restoring ||
                shotgun.BarrelRestoreAt is not { } restoreAt ||
                now < restoreAt)
            {
                continue;
            }

            shotgun.BarrelRestoreAt = null;
            shotgun.BarrelVisualState = BuckshotRouletteBarrelVisualState.Intact;
            Dirty(uid, shotgun);
        }
    }

    private void OnShotgunExamined(Entity<BuckshotRouletteShotgunComponent> shotgun, ref ExaminedEvent args)
    {
        var mode = GetFireModeLocString(shotgun.Comp.FireMode);
        args.PushMarkup(Loc.GetString("buckshot-roulette-fire-mode-examine", ("mode", mode)));
    }

    private string GetFireModeLocString(BuckshotRouletteFireMode mode)
    {
        return Loc.GetString(mode == BuckshotRouletteFireMode.Self
            ? "buckshot-roulette-fire-mode-self"
            : "buckshot-roulette-fire-mode-target");
    }

    private void OnShotgunGetVerbs(
        Entity<BuckshotRouletteShotgunComponent> shotgun,
        ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("buckshot-roulette-fire-mode-toggle-verb"),
            Priority = 1,
            Act = () => ToggleFireMode(shotgun, user),
        });
    }

    private void ToggleFireMode(Entity<BuckshotRouletteShotgunComponent> shotgun, EntityUid user)
    {
        shotgun.Comp.FireMode = shotgun.Comp.FireMode == BuckshotRouletteFireMode.Target
            ? BuckshotRouletteFireMode.Self
            : BuckshotRouletteFireMode.Target;
        DirtyField(shotgun.AsNullable(), nameof(BuckshotRouletteShotgunComponent.FireMode));

        var mode = GetFireModeLocString(shotgun.Comp.FireMode);
        _popup.PopupClient(Loc.GetString("buckshot-roulette-fire-mode-changed", ("mode", mode)), user, user);
    }

    private void OnShotgunFired(Entity<BuckshotRouletteShotgunComponent> shotgun, ref GunShotEvent args)
    {
        if (shotgun.Comp.FireMode != BuckshotRouletteFireMode.Self || Deleted(args.User) ||
            !TryComp<GunComponent>(shotgun, out var gun))
        {
            return;
        }

        var doubleDamage = shotgun.Comp.DoubleNextShot;

        // Усиление необходимо погасить до нанесения урона: переход стрелка в крит может
        // синхронно выбросить дробовик и запустить обработчик восстановления ствола.
        if (doubleDamage)
            ConsumeSawEffect(shotgun);

        foreach (var (shellUid, _) in args.Ammo)
        {
            if (shellUid is not { } shell || !TryComp<BuckshotRouletteShellComponent>(shell, out var rouletteShell))
                continue;

            if (rouletteShell.Live)
            {
                _audio.PlayPredicted(gun.SoundGunshotModified, shotgun, args.User);
                var damage = new DamageSpecifier(shotgun.Comp.SelfDamage);
                if (doubleDamage)
                    damage *= shotgun.Comp.SawDamageMultiplier;

                _damageable.TryChangeDamage(
                    args.User,
                    damage,
                    ignoreResistances: true,
                    origin: shotgun.Owner);
            }
            else
            {
                _audio.PlayPredicted(gun.SoundEmpty, shotgun, args.User);
            }

            // При самовыстреле штатное создание снаряда отменено, поэтому использованный патрон удаляется здесь.
            QueueDel(shell);
        }
    }

    private void OnShotgunAmmoShot(Entity<BuckshotRouletteShotgunComponent> shotgun, ref AmmoShotEvent args)
    {
        if (shotgun.Comp.FireMode != BuckshotRouletteFireMode.Target || !shotgun.Comp.DoubleNextShot)
            return;

        // У холостого патрона список снарядов пуст: усиление просто расходуется без лишней обработки нулевого урона.
        foreach (var projectileUid in args.FiredProjectiles)
        {
            if (!TryComp<ProjectileComponent>(projectileUid, out var projectile))
                continue;

            projectile.Damage *= shotgun.Comp.SawDamageMultiplier;
            Dirty(projectileUid, projectile);
        }

        ConsumeSawEffect(shotgun);
    }

    private void OnShotgunDropped(Entity<BuckshotRouletteShotgunComponent> shotgun, ref DroppedEvent args)
    {
        TryStartBarrelRestoration(shotgun);
    }

    private void TryStartBarrelRestoration(Entity<BuckshotRouletteShotgunComponent> shotgun)
    {
        if (!shotgun.Comp.BarrelRestorationPending ||
            shotgun.Comp.BarrelVisualState != BuckshotRouletteBarrelVisualState.Sawed)
        {
            return;
        }

        shotgun.Comp.BarrelRestorationPending = false;
        shotgun.Comp.BarrelVisualState = BuckshotRouletteBarrelVisualState.Restoring;
        shotgun.Comp.BarrelRestoreAt = _timing.CurTime + shotgun.Comp.BarrelRestoreDuration;
        Dirty(shotgun);
    }

    private void ConsumeSawEffect(Entity<BuckshotRouletteShotgunComponent> shotgun)
    {
        shotgun.Comp.DoubleNextShot = false;
        shotgun.Comp.BarrelRestorationPending = true;

        // Обычно восстановление запускает DroppedEvent. Если оружие уже успели выбить
        // до GunShotEvent, оно больше не находится в контейнере рук и ждать нового события нельзя.
        if (!_containers.IsEntityInContainer(shotgun))
        {
            TryStartBarrelRestoration(shotgun);
            return;
        }

        Dirty(shotgun);
    }

    private void OnBeerIngested(Entity<BuckshotRouletteBeerComponent> beer, ref IngestedEvent args)
    {
        if (beer.Comp.RewardGranted ||
            !_solution.ResolveSolution(beer.Owner, beer.Comp.SolutionName, ref beer.Comp.Solution, out var solution) ||
            solution.Volume > 0)
        {
            return;
        }

        beer.Comp.RewardGranted = true;
        var permit = EnsureComp<BuckshotRouletteShellPullPermitComponent>(args.Target);
        permit.Charges++;
        _popup.PopupClient(Loc.GetString("buckshot-roulette-beer-permit-granted"), args.Target, args.Target);
    }

    private void OnShotgunUseInHand(Entity<BuckshotRouletteShotgunComponent> shotgun, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<BuckshotRouletteShellPullPermitComponent>(args.User, out var permit))
            return;

        // RemCompDeferred удаляет исчерпанное разрешение в конце тика; эта проверка
        // не даёт повторному вводу за тот же тик извлечь дополнительный патрон.
        if (permit.Charges <= 0)
        {
            RemCompDeferred<BuckshotRouletteShellPullPermitComponent>(args.User);
            return;
        }

        args.Handled = true;
        args.ApplyDelay = false;

        if (!TryComp<BallisticAmmoProviderComponent>(shotgun, out var provider) || provider.Entities.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("buckshot-roulette-beer-pull-empty"), args.User, args.User);
            return;
        }

        var extracted = new List<(EntityUid? Entity, IShootable Shootable)>();
        var takeAmmo = new TakeAmmoEvent(1, extracted, Transform(args.User).Coordinates, args.User);
        RaiseLocalEvent(shotgun.Owner, takeAmmo);

        if (extracted.Count == 0 || extracted[0].Entity is not { } shell)
            return;

        _transform.SetCoordinates(shell, Transform(args.User).Coordinates);
        var ejectSound = TryComp<CartridgeAmmoComponent>(shell, out var cartridge)
            ? cartridge.EjectSound ?? shotgun.Comp.ShellEjectSound
            : shotgun.Comp.ShellEjectSound;

        // Это серверное взаимодействие не предсказывается клиентом,
        // поэтому звук принудительно отправляется всем поблизости.
        _audio.PlayPvs(
            ejectSound,
            shotgun,
            AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-1f));

        var cycled = new GunCycledEvent();
        RaiseLocalEvent(shotgun.Owner, ref cycled);

        permit.Charges--;
        if (permit.Charges <= 0)
            RemCompDeferred<BuckshotRouletteShellPullPermitComponent>(args.User);

        _popup.PopupClient(Loc.GetString("buckshot-roulette-beer-pull-success"), args.User, args.User);
    }

    private void OnAmmoCaseMapInit(Entity<BuckshotRouletteAmmoCaseComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage))
            return;

        var count = _random.Next(ent.Comp.MinimumShells, ent.Comp.MaximumShells + 1);
        var liveCount = _random.Next(1, count);

        var liveShells = Enumerable.Repeat(ent.Comp.LiveShell, liveCount);
        var blankShells = Enumerable.Repeat(ent.Comp.BlankShell, count - liveCount);
        var shells = liveShells.Concat(blankShells).ToList();
        _random.Shuffle(shells);

        var grid = storage.Grid.GetBoundingBox();
        var positions = new List<Vector2i>(storage.Grid.GetArea());
        for (var y = grid.Bottom; y <= grid.Top; y++)
        {
            for (var x = grid.Left; x <= grid.Right; x++)
            {
                if (storage.Grid.Contains(x, y))
                    positions.Add(new Vector2i(x, y));
            }
        }
        _random.Shuffle(positions);

        for (var i = 0; i < shells.Count; i++)
        {
            var shell = Spawn(shells[i], Transform(ent).Coordinates);
            if (!TryComp<ItemComponent>(shell, out var item) ||
                !_storage.InsertAt((ent.Owner, storage), (shell, item),
                    new ItemStorageLocation(Angle.Zero, positions[i]), out _, playSound: false))
            {
                QueueDel(shell);
            }
        }
    }

    private void OnItemCaseMapInit(Entity<BuckshotRouletteItemCaseComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage) || ent.Comp.Items.Count == 0)
            return;

        var minimum = Math.Max(1, ent.Comp.MinimumItems);
        var maximum = Math.Max(minimum, ent.Comp.MaximumItems);
        var count = _random.Next(minimum, maximum + 1);

        for (var i = 0; i < count; i++)
        {
            var item = Spawn(_random.Pick(ent.Comp.Items), Transform(ent).Coordinates);
            if (!_storage.Insert(ent.Owner, item, out _, storageComp: storage, playSound: false))
                QueueDel(item);
        }
    }

    private void OnAmmoCaseAfterInteract(Entity<BuckshotRouletteAmmoCaseComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target ||
            !TryComp<StorageComponent>(ent, out var storage) ||
            !TryComp<BuckshotRouletteShotgunComponent>(target, out _) ||
            !TryComp<BallisticAmmoProviderComponent>(target, out var provider))
        {
            return;
        }

        var shells = storage.Container.ContainedEntities
            .Where(HasComp<BuckshotRouletteShellComponent>)
            .ToList();
        _random.Shuffle(shells);

        var loaded = 0;
        foreach (var shell in shells)
        {
            if (!_gun.TryBallisticInsert((target, provider), shell, args.User, suppressInsertionSound: true))
                continue;

            loaded++;
        }

        if (loaded == 0)
            return;

        // Перенос патронов выполняется только на сервере, поэтому один раз отправляем штатный
        // звук зарядки всем поблизости, включая игрока, использовавшего кейс.
        _audio.PlayPvs(provider.SoundInsert, target);
        args.Handled = true;
    }

    private void OnToolAfterInteract(Entity<BuckshotRouletteToolComponent> tool, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target ||
            !TryComp<BuckshotRouletteShotgunComponent>(target, out var shotgun) ||
            !TryComp<BallisticAmmoProviderComponent>(target, out var provider))
        {
            return;
        }

        switch (tool.Comp.Effect)
        {
            case BuckshotRouletteToolEffect.BurnerPhone:
                UseBurnerPhone(args.User, provider);
                AnnounceUse(args.User, "buckshot-roulette-announcement-phone");
                break;
            case BuckshotRouletteToolEffect.MagnifyingGlass:
                UseMagnifyingGlass(args.User, provider);
                AnnounceUse(args.User, "buckshot-roulette-announcement-magnifier");
                break;
            case BuckshotRouletteToolEffect.Inverter:
                if (!TryUseInverter(args.User, target, shotgun, provider))
                    return;
                AnnounceUse(args.User, "buckshot-roulette-announcement-inverter");
                break;
            case BuckshotRouletteToolEffect.HandSaw:
                if (!TryUseHandSaw(tool.Owner, args.User, target, shotgun))
                    return;
                AnnounceUse(args.User, "buckshot-roulette-announcement-saw");
                break;
            default:
                return;
        }

        args.Handled = true;
        QueueDel(tool);
    }

    private void OnToolUseInHand(Entity<BuckshotRouletteToolComponent> tool, ref UseInHandEvent args)
    {
        var loc = tool.Comp.Effect switch
        {
            BuckshotRouletteToolEffect.Jammer => "buckshot-roulette-announcement-jammer",
            BuckshotRouletteToolEffect.Remote => "buckshot-roulette-announcement-remote",
            _ => null,
        };

        if (loc == null)
            return;

        AnnounceUse(args.User, loc);
        args.Handled = true;
        QueueDel(tool);
    }

    private void UseBurnerPhone(EntityUid user, BallisticAmmoProviderComponent provider)
    {
        if (provider.Entities.Count <= 2)
        {
            SendPrivate(user, Loc.GetString("buckshot-roulette-phone-no-information"));
            return;
        }

        var offset = _random.Next(1, provider.Entities.Count);
        var shell = provider.Entities[provider.Entities.Count - 1 - offset];
        if (!TryGetRouletteShell(user, shell, out var rouletteShell))
            return;

        var shellName = Loc.GetString(rouletteShell.Live
            ? "buckshot-roulette-shell-type-live"
            : "buckshot-roulette-shell-type-blank");
        SendPrivate(user, Loc.GetString("buckshot-roulette-phone-result",
            ("position", offset + 1),
            ("shell", shellName)));
    }

    private void UseMagnifyingGlass(EntityUid user, BallisticAmmoProviderComponent provider)
    {
        if (provider.Entities.Count == 0)
        {
            SendPrivate(user, Loc.GetString("buckshot-roulette-magnifier-empty"));
            return;
        }

        var shell = provider.Entities[^1];
        if (!TryGetRouletteShell(user, shell, out var rouletteShell))
            return;

        SendPrivate(user, Loc.GetString(rouletteShell.Live
            ? "buckshot-roulette-magnifier-live"
            : "buckshot-roulette-magnifier-blank"));
    }

    private bool TryUseInverter(
        EntityUid user,
        EntityUid shotgunUid,
        BuckshotRouletteShotgunComponent shotgun,
        BallisticAmmoProviderComponent provider)
    {
        if (provider.Entities.Count == 0)
        {
            SendPrivate(user, Loc.GetString("buckshot-roulette-inverter-empty"));
            return false;
        }

        var expectedCurrent = provider.Entities[^1];
        if (!TryGetRouletteShell(user, expectedCurrent, out var rouletteShell))
            return false;

        var extracted = new List<(EntityUid? Entity, IShootable Shootable)>();
        var takeAmmo = new TakeAmmoEvent(1, extracted, Transform(shotgunUid).Coordinates, user);
        RaiseLocalEvent(shotgunUid, takeAmmo);

        if (extracted.Count == 0 || extracted[0].Entity is not { } current)
            return false;

        if (current != expectedCurrent)
        {
            Log.Error($"Expected to extract {ToPrettyString(expectedCurrent)}, " +
                      $"but got {ToPrettyString(current)} from {ToPrettyString(shotgunUid)}.");
            _gun.TryBallisticInsert((shotgunUid, provider), current, user, suppressInsertionSound: true);
            return false;
        }

        var replacementPrototype = rouletteShell.Live ? shotgun.BlankShell : shotgun.LiveShell;
        var replacement = Spawn(replacementPrototype, Transform(shotgunUid).Coordinates);
        if (_gun.TryBallisticInsert((shotgunUid, provider), replacement, user, suppressInsertionSound: true))
        {
            QueueDel(current);
            return true;
        }

        QueueDel(replacement);

        // При ошибке конфигурации не теряем исходный патрон и не расходуем инвертор.
        if (!_gun.TryBallisticInsert((shotgunUid, provider), current, user, suppressInsertionSound: true))
        {
            Log.Error($"Failed to restore {ToPrettyString(current)} into " +
                      $"{ToPrettyString(shotgunUid)} after inverter failure.");
        }

        return false;
    }

    private bool TryGetRouletteShell(
        EntityUid user,
        EntityUid shell,
        out BuckshotRouletteShellComponent component)
    {
        if (TryComp<BuckshotRouletteShellComponent>(shell, out var shellComponent))
        {
            component = shellComponent;
            return true;
        }

        component = default!;
        Log.Error($"Buckshot Roulette ammo provider contains incompatible shell {ToPrettyString(shell)}.");
        SendPrivate(user, Loc.GetString("buckshot-roulette-invalid-shell"));
        return false;
    }

    private bool TryUseHandSaw(
        EntityUid sawUid,
        EntityUid user,
        EntityUid shotgunUid,
        BuckshotRouletteShotgunComponent shotgun)
    {
        if (shotgun.DoubleNextShot || shotgun.BarrelVisualState != BuckshotRouletteBarrelVisualState.Intact)
        {
            SendPrivate(user, Loc.GetString("buckshot-roulette-saw-already-active"));
            return false;
        }

        shotgun.DoubleNextShot = true;
        shotgun.BarrelVisualState = BuckshotRouletteBarrelVisualState.Sawed;
        Dirty(shotgunUid, shotgun);

        // Обработчик работает только на сервере, поэтому звук отправляется всем игрокам поблизости напрямую.
        if (TryComp<ToolComponent>(sawUid, out var tool) && tool.UseSound != null)
            _audio.PlayPvs(tool.UseSound, sawUid);

        return true;
    }

    private void AnnounceUse(EntityUid user, string loc)
    {
        _chat.TrySendInGameICMessage(user, Loc.GetString(loc), InGameICChatType.Emote, false,
            checkRadioPrefix: false, ignoreActionBlocker: true);
    }

    private void SendPrivate(EntityUid user, string message)
    {
        if (!_players.TryGetSessionByEntity(user, out var session))
            return;

        _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, EntityUid.Invalid, false, session.Channel);
    }
}
