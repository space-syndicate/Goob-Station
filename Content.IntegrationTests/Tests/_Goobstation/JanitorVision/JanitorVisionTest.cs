// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using System.Linq;
using Content.Client.Options.UI;
using Content.Client.Options.UI.Tabs;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Robust.Shared.Configuration;
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

    [Test]
    public async Task IntensityPreferenceLoadsInOptionsAndUpdatesScanner()
    {
        await Client.WaitAssertion(() =>
        {
            var config = Client.ResolveDependency<IConfigurationManager>();
            var oldValue = config.GetCVar(GoobCVars.JanitorVisionIntensity);
            AccessibilityTab tab = null;
            try
            {
                config.SetCVar(GoobCVars.JanitorVisionIntensity, 0.25f);
                tab = new AccessibilityTab();
                var slider = tab.FindControl<OptionSlider>("JanitorVisionIntensitySlider");
                Assert.That(slider.Slider.Value, Is.EqualTo(0.25f).Within(0.001f));
                var overlay = new JanitorVisionOverlay();
                Assert.That(overlay.Intensity, Is.EqualTo(0.25f));

                config.SetCVar(GoobCVars.JanitorVisionIntensity, 0f);
                tab.Control.ReloadValues();
                Assert.That(slider.Slider.Value, Is.Zero);
                Assert.That(overlay.Intensity, Is.Zero);
                config.SetCVar(GoobCVars.JanitorVisionIntensity, 1f);
                Assert.That(overlay.Intensity, Is.EqualTo(1f));
            }
            finally
            {
                config.SetCVar(GoobCVars.JanitorVisionIntensity, oldValue);
                tab?.Orphan();
            }
        });
    }

    [Test]
    public async Task FootprintsSyncAndCleanWhileEvaporatingPuddlesAreExcluded()
    {
        var decals = Server.System<Content.Server.Decals.DecalSystem>();
        var vision = Server.System<SharedJanitorVisionSystem>();
        uint footId = 0;
        uint bodyId = 0;
        NetEntity gridNet = default;
        NetEntity waterNet = default;
        await Server.WaitAssertion(() =>
        {
            var grid = MapData.Grid.Owner;
            gridNet = SEntMan.GetNetEntity(grid);
            var origin = Transform.WithEntityId(SEntMan.GetCoordinates(PlayerCoords), grid);
            var tile = new Tile(TileMan[Plating].TileId);
            for (var x = 0; x <= 4; x++)
                MapSystem.SetTile(MapData.Grid, origin.Offset(new Vector2(x, 0f)), tile);
            SEntMan.SpawnEntity("WallSolid", origin.Offset(new Vector2(1f, 0f)));

            Assert.That(decals.TryAddDecal("FootprintForward", origin.Offset(new Vector2(1.5f, -0.5f)),
                out footId, cleanable: true), Is.True);
            Assert.That(decals.TryAddDecal("FootprintBody", origin.Offset(new Vector2(2.5f, -0.5f)),
                out bodyId, cleanable: true), Is.True);
            var foot = ReadDecal(SEntMan, grid, footId);
            var body = ReadDecal(SEntMan, grid, bodyId);
            Assert.That(vision.TryGetFootprintTarget(SPlayer, grid, foot, 8f, out _), Is.True);
            Assert.That(vision.TryGetFootprintTarget(SPlayer, grid, body, 8f, out _), Is.True);
            Assert.That(vision.TryGetFootprintTarget(SPlayer, grid, foot, 1f, out _), Is.False);
            Assert.That(vision.TryGetFootprintTarget(SPlayer, grid, foot.WithCleanable(false), 8f, out _), Is.False);

            var water = new Solution("Water", FixedPoint2.New(40));
            Assert.That(Server.System<PuddleSystem>().TrySpillAt(origin.Offset(new Vector2(4f, 0f)),
                water, out var waterUid), Is.True);
            waterNet = SEntMan.GetNetEntity(waterUid);
            Assert.That(SEntMan.HasComponent<EvaporationComponent>(waterUid), Is.True);
            Assert.That(vision.TryGetTarget(SPlayer, waterUid, 8f, out _, out _), Is.False);
        });
        await RunTicks(10);

        await Client.WaitAssertion(() =>
        {
            var grid = CEntMan.GetEntity(gridNet);
            var clientVision = Client.System<SharedJanitorVisionSystem>();
            Assert.That(clientVision.TryGetFootprintTarget(CPlayer, grid, ReadDecal(CEntMan, grid, footId),
                8f, out _), Is.True);
            Assert.That(clientVision.TryGetFootprintTarget(CPlayer, grid, ReadDecal(CEntMan, grid, bodyId),
                8f, out _), Is.True);
            var water = CEntMan.GetEntity(waterNet);
            Assert.That(CEntMan.HasComponent<EvaporationComponent>(water), Is.True);
            Assert.That(clientVision.TryGetTarget(CPlayer, water, 8f, out _, out _), Is.False);
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(decals.RemoveDecal(MapData.Grid.Owner, footId), Is.True);
            Assert.That(decals.RemoveDecal(MapData.Grid.Owner, bodyId), Is.True);
        });
        await RunTicks(10);
        await Client.WaitAssertion(() =>
        {
            var comp = CEntMan.GetComponent<DecalGridComponent>(CEntMan.GetEntity(gridNet));
            Assert.That(comp.ChunkCollection.ChunkCollection.Values.Any(chunk => chunk.Decals.ContainsKey(footId)), Is.False);
            Assert.That(comp.ChunkCollection.ChunkCollection.Values.Any(chunk => chunk.Decals.ContainsKey(bodyId)), Is.False);
        });
    }

    private static Decal ReadDecal(IEntityManager entities, EntityUid grid, uint id)
    {
        var comp = entities.GetComponent<DecalGridComponent>(grid);
        foreach (var chunk in comp.ChunkCollection.ChunkCollection.Values)
        {
            if (chunk.Decals.TryGetValue(id, out var decal))
                return decal;
        }
        Assert.Fail($"Decal {id} is missing from the grid");
        return null;
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
