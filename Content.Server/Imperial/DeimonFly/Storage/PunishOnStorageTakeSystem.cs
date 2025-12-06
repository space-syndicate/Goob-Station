using System;
using Content.Shared.Damage;
using Content.Shared.Imperial.DeimonFly.Storage;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.DeimonFly.Storage;

public sealed class PunishOnStorageTakeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeAllEvent<StorageInteractWithItemEvent>(OnStorageInteract);
    }

    private void OnStorageInteract(StorageInteractWithItemEvent msg, EntitySessionEventArgs args)
    {
        // должен быть игрок, который кликнул по предмету
        if (args.SenderSession?.AttachedEntity is not { } userUid || Deleted(userUid))
            return;

        if (!TryGetEntity(msg.StorageUid, out EntityUid? storageUid) || storageUid is null)
            return;

        if (!TryComp(storageUid, out PunishOnStorageTakeComponent? comp))
            return;

        var now = _timing.CurTime;
        if (comp.Cooldown > TimeSpan.Zero && now < comp.LastPunish + comp.Cooldown)
            return;

        if (!TryGetEntity(msg.InteractedItemUid, out EntityUid? itemUid) || itemUid is null)
            return;

        if (comp.TargetItems.Count > 0)
        {
            if (!TryComp<MetaDataComponent>(itemUid, out var meta) ||
                meta.EntityPrototype?.ID is not { } protoId ||
                !comp.TargetItems.Contains(protoId))
            {
                return;
            }
        }

        var changed = _damageable.TryChangeDamage(userUid, comp.Damage, ignoreResistances: true, interruptsDoAfters: false);
        if (changed == null)
            return;

        if (comp.Sound != null)
            _audio.PlayPvs(comp.Sound, storageUid.Value);

        if (comp.Popup != null)
            _popup.PopupEntity(Loc.GetString(comp.Popup), storageUid.Value, userUid);

        comp.LastPunish = now;
        Dirty(storageUid.Value, comp);
    }
}
