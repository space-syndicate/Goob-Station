// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Client.JanitorVision;
using Content.Goobstation.Shared.JanitorVision;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Goobstation.JanitorVision;

public sealed class JanitorVisionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task EquipToggleAndRemove()
    {
        var inventory = Server.System<InventorySystem>();
        var actions = Server.System<SharedActionsSystem>();
        var glasses = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            glasses = SEntMan.SpawnEntity("ClothingEyesGlassesJanitor", SEntMan.GetCoordinates(PlayerCoords));
            Assert.That(inventory.TryEquip(SPlayer, glasses, "eyes", force: true), Is.True);
            var comp = SEntMan.GetComponent<JanitorVisionComponent>(glasses);
            Assert.That(comp.Enabled, Is.True);
            Assert.That(comp.ToggleActionEntity, Is.Not.Null);
        });
        await RunTicks(10);
        await AssertOverlay(true);

        async Task Toggle()
        {
            await Server.WaitAssertion(() =>
            {
                var comp = SEntMan.GetComponent<JanitorVisionComponent>(glasses);
                var action = actions.GetAction(comp.ToggleActionEntity);
                Assert.That(action, Is.Not.Null);
                actions.SetUseDelay(action.Value, null);
                actions.PerformAction(SPlayer, action.Value);
            });
            await RunTicks(10);
        }

        await Toggle();
        await AssertOverlay(false);
        await Toggle();
        await AssertOverlay(true);

        await Server.WaitAssertion(() => Assert.That(inventory.TryUnequip(SPlayer, "eyes", force: true), Is.True));
        await RunTicks(10);
        await AssertOverlay(false);

        // A stale/forged toggle while the goggles are on the floor must be rejected.
        await Server.WaitAssertion(() =>
        {
            var ev = new ToggleJanitorVisionEvent { Performer = SPlayer };
            SEntMan.EventBus.RaiseLocalEvent(glasses, ev);
            Assert.That(ev.Handled, Is.False);
        });

        // Merely holding them must not enable the HUD.
        await Server.WaitAssertion(() => Assert.That(HandSys.TryPickupAnyHand(SPlayer, glasses), Is.True));
        await RunTicks(10);
        await AssertOverlay(false);
    }

    [Test]
    public async Task ClientReceivesTargetsBehindWallAndRemovesCleanedTargets()
    {
        var trash = EntityUid.Invalid;
        var puddle = EntityUid.Invalid;
        NetEntity trashNet = default;
        NetEntity puddleNet = default;
        await Server.WaitAssertion(() =>
        {
            var origin = Transform.WithEntityId(SEntMan.GetCoordinates(PlayerCoords), MapData.Grid.Owner);
            var tile = new Tile(TileMan[Plating].TileId);
            for (var x = 0; x <= 3; x++)
                MapSystem.SetTile(MapData.Grid, origin.Offset(new Vector2(x, 0f)), tile);

            SEntMan.SpawnEntity("WallSolid", origin.Offset(new Vector2(1f, 0f)));
            trash = SEntMan.SpawnEntity("FoodPacketChipsTrash", origin.Offset(new Vector2(2f, 0f)));
            puddle = SEntMan.SpawnEntity("PuddleVomit", origin.Offset(new Vector2(3f, 0f)));
            trashNet = SEntMan.GetNetEntity(trash);
            puddleNet = SEntMan.GetNetEntity(puddle);
            var glasses = SEntMan.SpawnEntity("ClothingEyesGlassesJanitor", origin);
            Assert.That(Server.System<InventorySystem>().TryEquip(SPlayer, glasses, "eyes", force: true), Is.True);
        });
        await RunTicks(10);

        var clientTrash = EntityUid.Invalid;
        var clientPuddle = EntityUid.Invalid;
        await Client.WaitAssertion(() =>
        {
            clientTrash = CEntMan.GetEntity(trashNet);
            clientPuddle = CEntMan.GetEntity(puddleNet);
            var vision = Client.System<SharedJanitorVisionSystem>();
            Assert.That(vision.TryGetTarget(CPlayer, clientTrash, 8f, out var kind, out _), Is.True);
            Assert.That(kind, Is.EqualTo(JanitorVisionTarget.Trash));
            Assert.That(vision.TryGetTarget(CPlayer, clientPuddle, 8f, out kind, out _), Is.True);
            Assert.That(kind, Is.EqualTo(JanitorVisionTarget.Puddle));
            var overlays = Client.ResolveDependency<IOverlayManager>();
            Assert.That(overlays.HasOverlay<JanitorVisionOverlay>(), Is.True);
            Assert.That(overlays.GetOverlay<JanitorVisionOverlay>().Space,
                Is.EqualTo(Robust.Shared.Enums.OverlaySpace.WorldSpace));
        });

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(trash);
            SEntMan.DeleteEntity(puddle);
        });
        await RunTicks(10);
        await Client.WaitAssertion(() =>
        {
            var vision = Client.System<SharedJanitorVisionSystem>();
            Assert.That(vision.TryGetTarget(CPlayer, clientTrash, 8f, out _, out _), Is.False);
            Assert.That(vision.TryGetTarget(CPlayer, clientPuddle, 8f, out _, out _), Is.False);
        });
    }

    private async Task AssertOverlay(bool enabled)
    {
        await Client.WaitAssertion(() =>
            Assert.That(Client.ResolveDependency<IOverlayManager>().HasOverlay<JanitorVisionOverlay>(), Is.EqualTo(enabled)));
    }

    [Test]
    public async Task DetectTrashAndPuddlesThroughWallsOnlyInRange()
    {
        var vision = Server.System<SharedJanitorVisionSystem>();
        await Server.WaitAssertion(() =>
        {
            var origin = Transform.WithEntityId(SEntMan.GetCoordinates(PlayerCoords), MapData.Grid.Owner);
            var tile = new Tile(TileMan[Plating].TileId);
            for (var x = 0; x <= 3; x++)
                MapSystem.SetTile(MapData.Grid, origin.Offset(new Vector2(x, 0f)), tile);
            var trash = SEntMan.SpawnEntity("FoodPacketChipsTrash", origin.Offset(new Vector2(2f, 0f)));
            var puddle = SEntMan.SpawnEntity("Puddle", origin.Offset(new Vector2(3f, 0f)));
            SEntMan.SpawnEntity("WallSolid", origin.Offset(new Vector2(1f, 0f)));
            var ordinary = SEntMan.SpawnEntity("MopItem", origin);
            var mob = SEntMan.SpawnEntity("MobHuman", origin);

            Assert.That(vision.TryGetTarget(SPlayer, trash, 8f, out var kind, out _), Is.True);
            Assert.That(kind, Is.EqualTo(JanitorVisionTarget.Trash));
            Assert.That(vision.TryGetTarget(SPlayer, puddle, 8f, out kind, out _), Is.True);
            Assert.That(kind, Is.EqualTo(JanitorVisionTarget.Puddle));
            Assert.That(vision.TryGetTarget(SPlayer, ordinary, 8f, out _, out _), Is.False);
            Assert.That(vision.TryGetTarget(SPlayer, mob, 8f, out _, out _), Is.False);
            Assert.That(vision.TryGetTarget(SPlayer, trash, 1f, out _, out _), Is.False);
            Assert.That(vision.TryGetTarget(SPlayer, trash, 0f, out _, out _), Is.False);

            // Collected trash is no longer a cleaning target.
            var bag = SEntMan.SpawnEntity(null, origin);
            var containers = Server.System<SharedContainerSystem>();
            var container = containers.EnsureContainer<Container>(bag, "janitor-test");
            Assert.That(containers.Insert(trash, container), Is.True);
            Assert.That(vision.TryGetTarget(SPlayer, trash, 8f, out _, out _), Is.False);
            Assert.That(containers.Remove(trash, container), Is.True);
            Transform.SetCoordinates(trash, origin);
            Assert.That(vision.TryGetTarget(SPlayer, trash, 8f, out _, out _), Is.True);

            Transform.SetCoordinates(trash, new EntityCoordinates(MapSystem.CreateMap(), Vector2.Zero));
            Assert.That(vision.TryGetTarget(SPlayer, trash, 8f, out _, out _), Is.False);
            MapSystem.DeleteMap(Transform.GetMapId(trash));
            SEntMan.DeleteEntity(puddle);
            Assert.That(vision.TryGetTarget(SPlayer, puddle, 8f, out _, out _), Is.False);
        });
    }
}
