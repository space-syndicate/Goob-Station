using Content.Server._CorvaxGoob.OfferItem;
using Content.Shared._CorvaxGoob.OfferItem;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Goobstation;

[TestFixture]
public sealed class OfferItemTest
{
    [Test]
    public async Task AcceptOfferTransfersItemAndResetsBoth()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var handsSys = entMan.System<SharedHandsSystem>();
        var offerSys = entMan.System<OfferItemSystem>();

        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        EntityUid a = default, b = default, item = default;
        HandsComponent? aHands = default;
        OfferItemComponent? compAAfter = null, compBAfter = null;

        await server.WaitPost(() =>
        {
            a = entMan.SpawnEntity("MobHuman", map.GridCoords);
            b = entMan.SpawnEntity("MobHuman", map.GridCoords);
            item = entMan.SpawnEntity("Crowbar", map.GridCoords);

            aHands = entMan.GetComponent<HandsComponent>(a);
            Assert.That(handsSys.TryPickup(a, item, aHands.ActiveHandId!), Is.True);
        });
        await pair.RunTicksSync(2);

        await server.WaitPost(() =>
        {
            var compA = entMan.GetComponent<OfferItemComponent>(a);
            var compB = entMan.GetComponent<OfferItemComponent>(b);

            offerSys.StartOffer(a, compA, item, aHands!.ActiveHandId!);
            offerSys.LinkOffer(a, compA, b, compB);

            Assert.Multiple(() =>
            {
                Assert.That(compA.IsInOfferMode, Is.False);
                Assert.That(compA.Item, Is.EqualTo(item));
                Assert.That(compA.Target, Is.EqualTo(b));
                Assert.That(compB.IsInReceiveMode, Is.True);
                Assert.That(compB.Target, Is.EqualTo(a));
            });

            offerSys.AcceptOffer(new Entity<OfferItemComponent>(b, compB));
        });
        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            Assert.That(handsSys.GetActiveItem(b), Is.EqualTo(item));

            compAAfter = entMan.GetComponent<OfferItemComponent>(a);
            compBAfter = entMan.GetComponent<OfferItemComponent>(b);

            Assert.Multiple(() =>
            {
                Assert.That(compAAfter.IsInOfferMode, Is.False);
                Assert.That(compAAfter.IsInReceiveMode, Is.False);
                Assert.That(compAAfter.Hand, Is.Null);
                Assert.That(compAAfter.Item, Is.Null);
                Assert.That(compAAfter.Target, Is.Null);

                Assert.That(compBAfter.IsInReceiveMode, Is.False);
                Assert.That(compBAfter.Hand, Is.Null);
                Assert.That(compBAfter.Item, Is.Null);
                Assert.That(compBAfter.Target, Is.Null);
            });

            // PVS-сериализация очищенных компонентов не должна бросать исключений.
            Assert.DoesNotThrow(() => entMan.GetComponentState(entMan.EventBus, compAAfter!, null, GameTick.Zero));
            Assert.DoesNotThrow(() => entMan.GetComponentState(entMan.EventBus, compBAfter!, null, GameTick.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MovingBeyondDistanceCancelsOffer()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var tSys = entMan.System<TransformSystem>();
        var handsSys = entMan.System<SharedHandsSystem>();
        var offerSys = entMan.System<OfferItemSystem>();

        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        EntityUid a = default, b = default, item = default;
        HandsComponent? aHands = default;

        await server.WaitPost(() =>
        {
            a = entMan.SpawnEntity("MobHuman", map.GridCoords);
            b = entMan.SpawnEntity("MobHuman", map.GridCoords);
            item = entMan.SpawnEntity("Crowbar", map.GridCoords);

            aHands = entMan.GetComponent<HandsComponent>(a);
            handsSys.TryPickup(a, item, aHands.ActiveHandId!);

            var compA = entMan.GetComponent<OfferItemComponent>(a);
            var compB = entMan.GetComponent<OfferItemComponent>(b);
            offerSys.StartOffer(a, compA, item, aHands.ActiveHandId!);
            offerSys.LinkOffer(a, compA, b, compB);
        });
        await pair.RunTicksSync(2);

        await server.WaitPost(() =>
        {
            // Перемещаем A далеко за пределы стандартного MaxOfferDistance (2) от B.
            var coords = tSys.GetMapCoordinates(b);
            var farCoords = new MapCoordinates(coords.Position + new System.Numerics.Vector2(10, 0), coords.MapId);
            tSys.SetCoordinates(a, tSys.ToCoordinates(farCoords));
        });
        await pair.RunTicksSync(2);

        await server.WaitPost(() =>
        {
            var compA = entMan.GetComponent<OfferItemComponent>(a);
            var compB = entMan.GetComponent<OfferItemComponent>(b);

            Assert.Multiple(() =>
            {
                Assert.That(compA.IsInOfferMode, Is.False);
                Assert.That(compA.IsInReceiveMode, Is.False);
                Assert.That(compA.Item, Is.Null);
                Assert.That(compA.Target, Is.Null);
                Assert.That(compB.IsInReceiveMode, Is.False);
                Assert.That(compB.Target, Is.Null);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task QueueDeleteItemResetsOfferImmediately()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var handsSys = entMan.System<SharedHandsSystem>();
        var offerSys = entMan.System<OfferItemSystem>();

        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        EntityUid a = default, b = default, item = default;
        HandsComponent? aHands = default;

        await server.WaitPost(() =>
        {
            a = entMan.SpawnEntity("MobHuman", map.GridCoords);
            b = entMan.SpawnEntity("MobHuman", map.GridCoords);
            item = entMan.SpawnEntity("Crowbar", map.GridCoords);

            aHands = entMan.GetComponent<HandsComponent>(a);
            handsSys.TryPickup(a, item, aHands.ActiveHandId!);

            var compA = entMan.GetComponent<OfferItemComponent>(a);
            var compB = entMan.GetComponent<OfferItemComponent>(b);
            offerSys.StartOffer(a, compA, item, aHands.ActiveHandId!);
            offerSys.LinkOffer(a, compA, b, compB);

            entMan.QueueDeleteEntity(item);
        });

        // Без долгого поллинга: сброс должен произойти уже к моменту завершения удаления.
        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var compA = entMan.GetComponent<OfferItemComponent>(a);
            var compB = entMan.GetComponent<OfferItemComponent>(b);

            Assert.Multiple(() =>
            {
                Assert.That(compA.IsInOfferMode, Is.False);
                Assert.That(compA.Item, Is.Null);
                Assert.That(compA.Target, Is.Null);
                Assert.That(compB.IsInReceiveMode, Is.False);
                Assert.That(compB.Target, Is.Null);
            });

            Assert.DoesNotThrow(() => entMan.GetComponentState(entMan.EventBus, compA, null, GameTick.Zero));
            Assert.DoesNotThrow(() => entMan.GetComponentState(entMan.EventBus, compB, null, GameTick.Zero));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeletingOfferingSideResetsPartner()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var handsSys = entMan.System<SharedHandsSystem>();
        var offerSys = entMan.System<OfferItemSystem>();

        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        EntityUid a = default, b = default, item = default;
        HandsComponent? aHands = default;

        await server.WaitPost(() =>
        {
            a = entMan.SpawnEntity("MobHuman", map.GridCoords);
            b = entMan.SpawnEntity("MobHuman", map.GridCoords);
            item = entMan.SpawnEntity("Crowbar", map.GridCoords);

            aHands = entMan.GetComponent<HandsComponent>(a);
            handsSys.TryPickup(a, item, aHands.ActiveHandId!);

            var compA = entMan.GetComponent<OfferItemComponent>(a);
            var compB = entMan.GetComponent<OfferItemComponent>(b);
            offerSys.StartOffer(a, compA, item, aHands.ActiveHandId!);
            offerSys.LinkOffer(a, compA, b, compB);

            Assert.That(compB.Target, Is.EqualTo(a));

            entMan.DeleteEntity(a);
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var compB = entMan.GetComponent<OfferItemComponent>(b);

            Assert.Multiple(() =>
            {
                Assert.That(compB.IsInReceiveMode, Is.False);
                Assert.That(compB.Item, Is.Null);
                Assert.That(compB.Target, Is.Null);
            });

            // Выжившая сторона должна сериализоваться чисто (регрессия: краш висячего EntityUid).
            Assert.DoesNotThrow(() => entMan.GetComponentState(entMan.EventBus, compB, null, GameTick.Zero));
        });

        await pair.CleanReturnAsync();
    }
}
