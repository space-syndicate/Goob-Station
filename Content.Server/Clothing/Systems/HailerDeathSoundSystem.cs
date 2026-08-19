using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Clothing.Systems;

public sealed class HailerDeathSoundSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
            .WithVolume(-3f);

        float randomPitch = _random.NextFloat(0.85f, 1.15f);
        audioParams = audioParams.WithPitchScale(randomPitch);

        
        _audio.PlayPvs(comp.Sound, args.Target, audioParams);
    }
}
