using Content.Shared.Imperial.NinjaHeadset.Components;
using Content.Shared.Imperial.NinjaHeadset.Events;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Examine;
using System.Text;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;

namespace Content.Server.Imperial.NinjaHeadset.Systems;

public sealed class NinjaHeadsetSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaHeadsetComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NinjaHeadsetComponent, NinjaHackDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<NinjaHeadsetComponent, ExaminedEvent>(OnExamine);
    }

    private string GetLocalizedChannelName(string frequencyId)
    {
        if (_prototypeManager.TryIndex<RadioChannelPrototype>(frequencyId, out var prototype))
        {
            return prototype.LocalizedName;
        }

        return frequencyId;
    }
    private void OnExamine(EntityUid uid, NinjaHeadsetComponent component, ExaminedEvent args)
    {
        if (component.CopiedFrequencies.Count == 0)
        {
            args.PushMarkup(Loc.GetString("ninja-headset-examine-empty"));
            return;
        }

        var message = new StringBuilder();
        message.AppendLine(Loc.GetString("ninja-headset-examine-copied"));

        foreach (var frequency in component.CopiedFrequencies)
        {
            var localizedName = GetLocalizedChannelName(frequency);
            message.AppendLine($"[color=yellow]- {localizedName}[/color]");
        }

        args.PushMarkup(message.ToString());
    }

    private void OnAfterInteract(EntityUid uid, NinjaHeadsetComponent component, AfterInteractEvent args)
    {
        if (!HasComp<SpaceNinjaComponent>(args.User))
            return;

        if (!args.CanReach || args.Target == null)
            return;

        if (!TryGetTargetHeadset(args.Target.Value, out var headsetUid) || !headsetUid.HasValue)
            return;

        if (args.Target == args.User)
            return;

        if (component.HackingTarget != null)
            return;

        component.HackingTarget = args.Target.Value;
        component.TargetHeadset = headsetUid.Value;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.CopyFrequenciesTime, new NinjaHackDoAfterEvent(), uid, target: args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            Broadcast = false
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            component.HackingTarget = null;
            component.TargetHeadset = null;
        }
    }

    private void OnDoAfter(EntityUid uid, NinjaHeadsetComponent component, NinjaHackDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.HackingTarget = null;
            component.TargetHeadset = null;
            return;
        }

        if (component.TargetHeadset == null || !EntityAlive(component.TargetHeadset.Value))
        {
            component.HackingTarget = null;
            component.TargetHeadset = null;
            return;
        }

        var newFrequencies = new HashSet<string>();
        var channelsChanged = false;
        EncryptionKeyHolderComponent? ninjaEncryption = null;

        if (TryComp<EncryptionKeyHolderComponent>(component.TargetHeadset.Value, out var targetEncryption) &&
            TryComp<EncryptionKeyHolderComponent>(uid, out ninjaEncryption))
        {
            foreach (var channel in targetEncryption.Channels)
            {
                if (!component.CopiedFrequencies.Contains(channel))
                {
                    component.CopiedFrequencies.Add(channel);
                    newFrequencies.Add(channel);

                    if (!ninjaEncryption.Channels.Contains(channel))
                    {
                        ninjaEncryption.Channels.Add(channel);
                        channelsChanged = true;
                    }
                }
            }
        }

        if (channelsChanged && ninjaEncryption != null)
            RaiseLocalEvent(uid, new EncryptionChannelsChangedEvent(ninjaEncryption));

        Dirty(uid, component);

        if (newFrequencies.Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("ninja-headset-copy-success"), uid, args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("ninja-headset-copy-none"), uid, args.User);
        }

        component.HackingTarget = null;
        component.TargetHeadset = null;
    }

    private bool TryGetTargetHeadset(EntityUid targetUid, out EntityUid? headsetUid)
    {
        headsetUid = null;

        if (HasComp<EncryptionKeyHolderComponent>(targetUid))
        {
            headsetUid = targetUid;
            return true;
        }

        if (_inventorySystem.TryGetSlotEntity(targetUid, "ears", out var earsItem) &&
            HasComp<EncryptionKeyHolderComponent>(earsItem))
        {
            headsetUid = earsItem;
            return true;
        }

        return false;
    }

    private bool EntityAlive(EntityUid uid)
    {
        return !Deleted(uid) && !Terminating(uid);
    }
}
