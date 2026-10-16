using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Shared._Aquila.Nutrition;
using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class SlimeVoiceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeVoiceComponent, InteractionSuccessEvent>(OnInteractionSuccess);
        SubscribeLocalEvent<SlimeVoiceComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<SlimeVoiceComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SlimeVoiceComponent, HungerThresholdChangedEvent>(OnHungerThresholdChanged);
    }

    private void OnInteractionSuccess(Entity<SlimeVoiceComponent> ent, ref InteractionSuccessEvent args)
    {
        if (_timing.CurTime >= ent.Comp.NextPettedSound)
            _audio.PlayPvs(ent.Comp.PettedSound, ent);

        ent.Comp.NextPettedSound = _timing.CurTime + ent.Comp.PettedCooldown;
    }

    private void OnDamageChanged(Entity<SlimeVoiceComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased
            || args.DamageDelta == null
            || args.DamageDelta.GetTotal() < ent.Comp.MinHurtDamage
            || _mobState.IsDead(ent))
            return;

        if (_timing.CurTime >= ent.Comp.NextHurtSound)
            _audio.PlayPvs(ent.Comp.HurtSound, ent);

        ent.Comp.NextHurtSound = _timing.CurTime + ent.Comp.HurtCooldown;
    }

    private void OnMobStateChanged(Entity<SlimeVoiceComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            _audio.PlayPvs(ent.Comp.DeathSound, ent);
    }

    private void OnHungerThresholdChanged(Entity<SlimeVoiceComponent> ent, ref HungerThresholdChangedEvent args)
    {
        if (_mobState.IsDead(ent))
            return;

        if (Crossed(args, HungerThreshold.Starving))
            _audio.PlayPvs(ent.Comp.StarvingSound, ent);
        else if (Crossed(args, HungerThreshold.Peckish))
            _audio.PlayPvs(ent.Comp.HungrySound, ent);
    }

    private static bool Crossed(HungerThresholdChangedEvent args, HungerThreshold threshold)
    {
        return args.Current <= threshold && args.Previous > threshold;
    }
}
