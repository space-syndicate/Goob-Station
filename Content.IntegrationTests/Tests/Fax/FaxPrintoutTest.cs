// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Fax;
using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fax.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Fax;

[TestFixture]
public sealed class FaxPrintoutTest
{
    [TestCase("HandheldFax", false, false, false, false)]
    [TestCase("HandheldFaxNTR", false, false, false, false)]
    [TestCase("HandheldFaxNanorep", false, false, false, false)]
    [TestCase("HandheldFaxNTR", true, true, false, false)]
    [TestCase("HandheldFaxNTR", true, true, true, false)]
    [TestCase("HandheldFaxNTR", true, false, false, false)]
    [TestCase("HandheldFaxNTR", true, true, false, true)]
    [TestCase("HandheldFaxOfficial", false, false, false, false)]
    [TestCase("FaxMachineBase", false, false, false, false)]
    public async Task ReceivePrintout(string prototype, bool occupied, bool withBackpack, bool full, bool held)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.EntMan;
        var slots = server.System<ItemSlotsSystem>();
        var storage = server.System<SharedStorageSystem>();
        var containers = server.System<SharedContainerSystem>();
        EntityUid fax = default;
        EntityUid backpack = default;
        EntityUid original = default;
        FaxMachineComponent component = null;
        const string content = "Fax printout regression test";

        await server.WaitAssertion(() =>
        {
            fax = entities.SpawnEntity(prototype, map.GridCoords);
            component = entities.GetComponent<FaxMachineComponent>(fax);
            entities.GetComponent<ApcPowerReceiverComponent>(fax).NeedsPower = false;
            if (occupied)
            {
                original = entities.SpawnEntity("Paper", map.GridCoords);
                Assert.That(slots.TryInsert(fax, component.PaperSlot, original, null), Is.True);
                foreach (var slot in component.OutputSlots)
                {
                    var existing = entities.SpawnEntity("Paper", map.GridCoords);
                    Assert.That(slots.TryInsert(fax, slot, existing, null), Is.True);
                }
            }

            if (withBackpack)
            {
                backpack = entities.SpawnEntity("ClothingBackpack", map.GridCoords);
                if (held)
                {
                    var human = entities.SpawnEntity("MobHuman", map.GridCoords);
                    Assert.That(server.System<InventorySystem>().TryEquip(human, backpack, "back", force: true), Is.True);
                    Assert.That(server.System<SharedHandsSystem>().TryPickupAnyHand(human, fax), Is.True);
                }
                else
                    Assert.That(storage.Insert(backpack, fax, out _), Is.True);

                if (full)
                {
                    var filled = false;
                    for (var i = 0; i < 100; i++)
                    {
                        var filler = entities.SpawnEntity("Paper", map.GridCoords);
                        if (storage.Insert(backpack, filler, out _))
                            continue;
                        entities.DeleteEntity(filler);
                        filled = true;
                        break;
                    }
                    Assert.That(filled, Is.True, "Backpack must actually be full");
                }
            }

            server.System<FaxSystem>().Receive(fax, new FaxPrintout(content, "Received document", prototypeId: "Paper", locked: true));
        });

        await pair.RunSeconds(6);
        await server.WaitAssertion(() =>
        {
            EntityUid? printed = null;
            var query = entities.EntityQueryEnumerator<PaperComponent>();
            while (query.MoveNext(out var uid, out var paper))
            {
                if (paper.Content != content)
                    continue;
                Assert.That(printed, Is.Null, "Exactly one document should be printed");
                printed = uid;
                Assert.That(paper.EditingDisabled, Is.True);
                Assert.That(entities.GetComponent<MetaDataComponent>(uid).EntityName, Is.EqualTo("Received document"));
            }
            Assert.That(printed, Is.Not.Null);
            Assert.That(component.PrintingQueue, Is.Empty);
            if (occupied)
                Assert.That(component.PaperSlot.Item, Is.EqualTo(original), "Do not replace the outgoing document");

            var retained = prototype != "FaxMachineBase" && !occupied;
            var inBackpack = occupied && withBackpack && !full;
            if (retained || inBackpack)
            {
                Assert.That(containers.TryGetContainingContainer((printed!.Value, null, null), out var container), Is.True);
                Assert.That(container!.Owner, Is.EqualTo(retained ? fax : backpack));
                if (retained)
                    Assert.That(slots.TryEject(fax, component.OutputSlots[0], null, out _), Is.True, "Received paper must be removable");
            }
            else
            {
                Assert.That(containers.IsEntityInContainer(printed!.Value), Is.False);
                Assert.That(entities.GetComponent<TransformComponent>(printed.Value).MapID, Is.EqualTo(map.MapId));
            }
        });
        await pair.CleanReturnAsync();
    }
}
