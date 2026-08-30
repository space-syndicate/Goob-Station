using Content.Shared._CorvaxGoob.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._CorvaxGoob.Clothing.System;

public sealed class HailerDeathSoundSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        
        if (!_inventory.TryGetSlotEntity(args.Target, "mask", out var maskUid))
            return;

        if (!TryComp<HailerDeathSoundComponent>(maskUid, out var comp) || comp.Sound == null)
            return;
        
        if (args.NewMobState != MobState.Dead)
        {
            comp.HasPlayed = false;
            return;
        }

        if (comp.HasPlayed)
            return;
      
        comp.HasPlayed = true;

        var audioParams = AudioParams.Default
            .WithVolume(-3f)
            .WithVariation(0.15f);

        _audio.PlayPvs(comp.Sound, args.Target, audioParams);
    }
}

