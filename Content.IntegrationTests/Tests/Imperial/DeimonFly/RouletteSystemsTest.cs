using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.DeimonFly.BuckshotRoulette;
using Content.Shared.Imperial.DeimonFly.RussianRoulette;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Imperial.DeimonFly;

[TestOf(typeof(RussianRouletteGunComponent))]
[TestOf(typeof(BuckshotRouletteShotgunComponent))]
public sealed class RouletteSystemsTest : InteractionTest
{
    protected override string PlayerPrototype => HumanPrototype;

    private const string HumanPrototype = "MobHuman";
    private const string RussianRouletteRevolver = "WeaponRevolverFinalBet";
    private const string UnrevivableRussianRouletteRevolver = "WeaponRevolverFinalBetUnrevivable";
    private const string DealerShotgun = "WeaponShotgunBuckshotRoulette";
    private const string LiveShell = "ShellShotgunBuckshotRouletteLive";
    private const string BlankShell = "ShellShotgunBuckshotRouletteBlank";
    private const string HandSaw = "BuckshotRouletteHandSaw";
    private const string MedicalKit = "BuckshotRouletteMedicalKit";

    [Test]
    public async Task RussianRouletteSpawnsOneCartridgeAndDamagesOnlyShooter()
    {
        await AddAtmosphere();
        var target = await SpawnTarget(HumanPrototype);
        var revolver = await PlaceInHands(RussianRouletteRevolver);
        var revolverUid = ToServer(revolver);
        var provider = SEntMan.GetComponent<RevolverAmmoProviderComponent>(revolverUid);

        await Server.WaitAssertion(() =>
        {
            var loadedSlots = provider.AmmoSlots
                .Select((uid, index) => (uid, index))
                .Where(entry => entry.uid != null)
                .ToList();

            Assert.That(loadedSlots, Has.Count.EqualTo(1),
                "Револьвер русской рулетки должен появляться ровно с одним патроном.");
            Assert.That(provider.Chambers.Count(chamber => chamber == true), Is.EqualTo(1),
                "Только одна камора должна содержать боевой патрон.");
            Assert.That(provider.Chambers[loadedSlots[0].index], Is.True,
                "Состояние выбранной каморы не соответствует помещённому в неё патрону.");
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(loadedSlots[0].uid!.Value).EntityPrototype?.ID,
                Is.EqualTo("CartridgeMagnumFinalBet"));
            Assert.That(SGun.GetAmmoCount(revolverUid), Is.EqualTo(1));

            // Убираем случайность только из самого выстрела:
            // генерация выше по-прежнему проверяется как случайная камора.
            provider.CurrentIndex = loadedSlots[0].index;
            SEntMan.Dirty(revolverUid, provider);
        });

        await RunSeconds(2f);
        await AttemptShoot(target);
        await RunTicks(3);

        Assert.Multiple(() =>
        {
            Assert.That(GetDamageOfType(SPlayer, "Piercing"), Is.EqualTo(FixedPoint2.New(200)),
                "Подтверждённый выстрел должен нанести стрелку ровно 200 урона.");
            Assert.That(GetDamageOfType(ToServer(target), "Piercing"), Is.EqualTo(FixedPoint2.Zero),
                "Безвредный внешний снаряд не должен наносить урон выбранной цели.");
            Assert.That(SEntMan.GetComponent<MobStateComponent>(SPlayer).CurrentState, Is.EqualTo(MobState.Dead));
            Assert.That(SEntMan.HasComponent<UnrevivableComponent>(SPlayer), Is.False,
                "Обычная версия револьвера не должна запрещать воскрешение.");
            Assert.That(SEntMan.HasComponent<StealTargetComponent>(revolverUid), Is.False,
                "Ивентовый револьвер не должен засчитываться как служебный пистолет для цели кражи.");
        });
    }

    [Test]
    public async Task UnrevivableRussianRouletteMarksOnlyADeadShooter()
    {
        await AddAtmosphere();
        var target = await SpawnTarget(HumanPrototype);
        await PlaceLoadedRevolver(UnrevivableRussianRouletteRevolver);

        await RunSeconds(2f);
        await AttemptShoot(target);
        await RunTicks(3);

        Assert.Multiple(() =>
        {
            Assert.That(SEntMan.GetComponent<MobStateComponent>(SPlayer).CurrentState, Is.EqualTo(MobState.Dead));
            Assert.That(SEntMan.HasComponent<UnrevivableComponent>(SPlayer), Is.True,
                "Безвозвратная версия должна запрещать воскрешение убитого стрелка.");
        });
    }

    [Test]
    public async Task UnrevivableRussianRouletteDoesNotMarkASurvivor()
    {
        await AddAtmosphere();
        var target = await SpawnTarget(HumanPrototype);
        await PlaceLoadedRevolver(UnrevivableRussianRouletteRevolver);

        await Server.WaitPost(() =>
        {
            var thresholds = SEntMan.System<MobThresholdSystem>();
            thresholds.SetMobStateThreshold(SPlayer, FixedPoint2.New(300), MobState.Dead);
        });

        await RunSeconds(2f);
        await AttemptShoot(target);
        await RunTicks(3);

        Assert.Multiple(() =>
        {
            Assert.That(SEntMan.GetComponent<MobStateComponent>(SPlayer).CurrentState,
                Is.EqualTo(MobState.Critical));
            Assert.That(SEntMan.HasComponent<UnrevivableComponent>(SPlayer), Is.False,
                "Временная метка должна сниматься, если 200 урона не убили нестандартную сущность.");
        });
    }

    [TestCase(LiveShell, BuckshotRouletteFireMode.Target, 0, 34,
        TestName = "Buckshot live shell damages target")]
    [TestCase(BlankShell, BuckshotRouletteFireMode.Target, 0, 0,
        TestName = "Buckshot blank shell damages nobody in target mode")]
    [TestCase(LiveShell, BuckshotRouletteFireMode.Self, 34, 0,
        TestName = "Buckshot live shell damages shooter in self mode")]
    [TestCase(BlankShell, BuckshotRouletteFireMode.Self, 0, 0,
        TestName = "Buckshot blank shell damages nobody in self mode")]
    public async Task BuckshotShellAndFireModeAreRespected(
        string shellPrototype,
        BuckshotRouletteFireMode mode,
        int expectedShooterDamage,
        int expectedTargetDamage)
    {
        await AddAtmosphere();
        var target = await SpawnTarget(HumanPrototype);
        var shotgun = await PlaceInHands(DealerShotgun);
        var shotgunUid = ToServer(shotgun);
        var roulette = SEntMan.GetComponent<BuckshotRouletteShotgunComponent>(shotgunUid);

        await Server.WaitPost(() =>
        {
            roulette.FireMode = mode;
            SEntMan.Dirty(shotgunUid, roulette);
        });
        await LoadShell(shotgunUid, shellPrototype);

        await UseInHand();
        Assert.That(SEntMan.GetComponent<WieldableComponent>(shotgunUid).Wielded, Is.True,
            "Дробовик должен быть взят в две руки перед выстрелом.");

        await RunSeconds(2f);
        await AttemptShoot(target);
        await RunSeconds(0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(GetDamageOfType(SPlayer, "Cellular"),
                Is.EqualTo(FixedPoint2.New(expectedShooterDamage)));
            Assert.That(GetDamageOfType(ToServer(target), "Cellular"),
                Is.EqualTo(FixedPoint2.New(expectedTargetDamage)));
            Assert.That(SGun.GetAmmoCount(shotgunUid), Is.Zero,
                "Использованный боевой или холостой патрон должен покинуть дробовик.");
        });
    }

    [Test]
    public async Task SawedTargetShotDoublesDamage()
    {
        await AddAtmosphere();
        var shotgun = await SpawnTarget(DealerShotgun);
        var shotgunUid = ToServer(shotgun);
        var roulette = SEntMan.GetComponent<BuckshotRouletteShotgunComponent>(shotgunUid);

        await InteractUsing(HandSaw);
        await Pickup(shotgun);
        await LoadShell(shotgunUid, LiveShell);
        await Server.WaitPost(() =>
        {
            roulette.FireMode = BuckshotRouletteFireMode.Target;
            SEntMan.Dirty(shotgunUid, roulette);
        });

        await UseInHand();
        var target = await SpawnTarget(HumanPrototype);
        await RunSeconds(2f);
        await AttemptShoot(target);
        await RunSeconds(0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(GetDamageOfType(SPlayer, "Cellular"), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(GetDamageOfType(ToServer(target), "Cellular"), Is.EqualTo(FixedPoint2.New(68)),
                "Пила должна удваивать урон боевого патрона в режиме стрельбы по цели.");
            Assert.That(SGun.GetAmmoCount(shotgunUid), Is.Zero);
            Assert.That(roulette.DoubleNextShot, Is.False);
            Assert.That(roulette.BarrelRestorationPending, Is.True);
        });
    }

    [Test]
    public async Task MedicalKitHealsOnlyCellularDamage()
    {
        var kit = await SpawnTarget(MedicalKit);
        var healing = SEntMan.GetComponent<HealingComponent>(ToServer(kit));

        Assert.Multiple(() =>
        {
            Assert.That(healing.Damage.DamageDict["Cellular"], Is.EqualTo(FixedPoint2.New(-34)));
            Assert.That(healing.Damage.DamageDict.Count(entry => entry.Value != FixedPoint2.Zero), Is.EqualTo(1),
                "Аптечка не должна наследовать лечение обычных травм от HardTraumapack1.");
        });
    }

    [Test]
    public async Task SawedSelfShotRestoresBarrelAfterCriticalDrop()
    {
        await AddAtmosphere();
        var shotgun = await SpawnTarget(DealerShotgun);
        var shotgunUid = ToServer(shotgun);
        var roulette = SEntMan.GetComponent<BuckshotRouletteShotgunComponent>(shotgunUid);
        var damage = SEntMan.System<DamageableSystem>();

        // Используем настоящую пилу, чтобы тест проверял не только вручную выставленное состояние компонента.
        await InteractUsing(HandSaw);
        Assert.Multiple(() =>
        {
            Assert.That(roulette.DoubleNextShot, Is.True);
            Assert.That(roulette.BarrelVisualState, Is.EqualTo(BuckshotRouletteBarrelVisualState.Sawed));
        });

        await Pickup(shotgun);
        await LoadShell(shotgunUid, LiveShell);
        await Server.WaitPost(() =>
        {
            roulette.FireMode = BuckshotRouletteFireMode.Self;
            SEntMan.Dirty(shotgunUid, roulette);

            // 68 предварительного урона гарантируют переход в крит от усиленного выстрела на 68.
            var preliminaryDamage = new Content.Shared.Damage.DamageSpecifier(roulette.SelfDamage) * 2f;
            damage.TryChangeDamage(SPlayer, preliminaryDamage, ignoreResistances: true);
        });
        Assert.That(SEntMan.GetComponent<MobStateComponent>(SPlayer).CurrentState, Is.EqualTo(MobState.Alive));

        await UseInHand();
        Assert.That(SEntMan.GetComponent<WieldableComponent>(shotgunUid).Wielded, Is.True);

        var target = await SpawnTarget(HumanPrototype);
        await RunSeconds(2f);
        await AttemptShoot(target);
        await RunTicks(3);

        Assert.Multiple(() =>
        {
            Assert.That(SEntMan.GetComponent<MobStateComponent>(SPlayer).CurrentState, Is.EqualTo(MobState.Critical),
                "Усиленный самовыстрел должен перевести предварительно раненого человека в крит.");
            Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.Null,
                "Переход в крит должен выбросить дробовик из рук.");
            Assert.That(roulette.DoubleNextShot, Is.False);
            Assert.That(roulette.BarrelRestorationPending, Is.False,
                "Синхронное выпадение при переходе в крит должно сразу запустить восстановление.");
            Assert.That(roulette.BarrelVisualState, Is.EqualTo(BuckshotRouletteBarrelVisualState.Restoring));
            Assert.That(GetDamageOfType(ToServer(target), "Cellular"), Is.EqualTo(FixedPoint2.Zero));
        });

        await RunSeconds((float) roulette.BarrelRestoreDuration.TotalSeconds + 0.2f);

        Assert.Multiple(() =>
        {
            Assert.That(roulette.BarrelVisualState, Is.EqualTo(BuckshotRouletteBarrelVisualState.Intact));
            Assert.That(roulette.BarrelRestoreAt, Is.Null);
        });
    }

    private async Task LoadShell(EntityUid shotgunUid, EntProtoId shellPrototype)
    {
        await Server.WaitAssertion(() =>
        {
            var provider = SEntMan.GetComponent<BallisticAmmoProviderComponent>(shotgunUid);
            var coordinates = SEntMan.GetComponent<TransformComponent>(shotgunUid).Coordinates;
            var shell = SEntMan.SpawnEntity(shellPrototype, coordinates);

            Assert.That(SGun.TryBallisticInsert(
                (shotgunUid, provider),
                shell,
                SPlayer,
                suppressInsertionSound: true),
                $"Не удалось загрузить {shellPrototype} в тестовый дробовик.");
        });
        await RunTicks(1);
    }

    private async Task<EntityUid> PlaceLoadedRevolver(EntProtoId prototype)
    {
        var revolver = await PlaceInHands(prototype);
        var revolverUid = ToServer(revolver);

        await Server.WaitPost(() =>
        {
            var provider = SEntMan.GetComponent<RevolverAmmoProviderComponent>(revolverUid);
            provider.CurrentIndex = provider.AmmoSlots.FindIndex(slot => slot != null);
            Assert.That(provider.CurrentIndex, Is.GreaterThanOrEqualTo(0));
            SEntMan.Dirty(revolverUid, provider);
        });

        return revolverUid;
    }

    private FixedPoint2 GetDamageOfType(EntityUid uid, string damageType)
    {
        var component = SEntMan.GetComponent<DamageableComponent>(uid);
        var damage = SEntMan.System<DamageableSystem>().GetPositiveDamage((uid, component));
        return damage.DamageDict.TryGetValue(damageType, out var amount)
            ? amount
            : FixedPoint2.Zero;
    }
}
