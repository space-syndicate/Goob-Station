// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Fax;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Fax;

public sealed class FaxTrayInteractionTest : InteractionTest
{
    [Test]
    public async Task FiveDocumentsAndReminder()
    {
        await SpawnTarget("HandheldFaxNTR");
        var fax = STarget!.Value;
        var entities = Server.EntMan;
        FaxMachineComponent component = null;
        const string sound = "/Audio/Machines/high_tech_confirm.ogg";
        int SoundCount() => entities.EntityQuery<AudioComponent>().Count(audio => audio.FileName == sound);

        await Server.WaitAssertion(() =>
        {
            component = entities.GetComponent<FaxMachineComponent>(fax);
            Assert.That(component.OutputSlots, Has.Count.EqualTo(5));
            // Keep each emitted source alive so an unexpected reminder between assertions cannot be missed.
            component.ReminderSound = new SoundPathSpecifier(sound, AudioParams.Default.WithLoop(true));
            for (var i = 0; i < 5; i++)
                Server.System<FaxSystem>().Receive(fax, new FaxPrintout($"Document {i}", $"Document {i}", prototypeId: "Paper"));
        });
        await RunSeconds(16);
        await Server.WaitAssertion(() =>
        {
            Assert.That(component.OutputSlots.Count(slot => slot.HasItem), Is.EqualTo(5));
            Assert.That(component.PaperSlot.HasItem, Is.False, "The sending slot must remain available");
            for (var i = 0; i < 5; i++)
                Assert.That(entities.GetComponent<PaperComponent>(component.OutputSlots[i].Item!.Value).Content, Is.EqualTo($"Document {i}"));
            Assert.That(SoundCount(), Is.Zero, "No reminder before 30 seconds with paper in the tray");
        });
        await RunSeconds(18);
        await Server.WaitAssertion(() => Assert.That(SoundCount(), Is.EqualTo(1)));
        await RunSeconds(30);
        await Server.WaitAssertion(() => Assert.That(SoundCount(), Is.EqualTo(2)));

        await Activate();
        await SendBui(FaxUiKey.Key, new FaxSetReminderMessage(false));
        await RunSeconds(35);
        await Server.WaitAssertion(() =>
        {
            Assert.That(component.ReminderEnabled, Is.False);
            Assert.That(SoundCount(), Is.EqualTo(2), "Muted fax must not emit reminders");
        });
        await CloseBui(FaxUiKey.Key);
        await Activate();
        await Server.WaitAssertion(() => Assert.That(component.ReminderEnabled, Is.False, "Mute survives reopening"));
        await SendBui(FaxUiKey.Key, new FaxEjectOutputMessage(-1));
        await SendBui(FaxUiKey.Key, new FaxEjectOutputMessage(5));
        await Server.WaitAssertion(() => Assert.That(component.OutputSlots.Count(slot => slot.HasItem), Is.EqualTo(5)));
        await SendBui(FaxUiKey.Key, new FaxEjectOutputMessage(2));
        await Server.WaitAssertion(() => Assert.That(component.OutputSlots[2].HasItem, Is.False));
        await SendBui(FaxUiKey.Key, new FaxSetReminderMessage(true));
        await RunSeconds(31);
        await Server.WaitAssertion(() => Assert.That(SoundCount(), Is.EqualTo(3), "Reminder resumes after enabling"));

        for (var i = 0; i < 5; i++)
            await SendBui(FaxUiKey.Key, new FaxEjectOutputMessage(i));
        await RunSeconds(35);
        await Server.WaitAssertion(() =>
        {
            Assert.That(component.OutputSlots.All(slot => !slot.HasItem), Is.True);
            Assert.That(SoundCount(), Is.EqualTo(3), "Empty fax must not emit reminders");
        });
    }
}
