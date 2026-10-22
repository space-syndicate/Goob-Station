using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared._CorvaxGoob.MartialArts.Components;
using Content.Goobstation.Shared._CorvaxGoob.MartialArts.Events;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Speech.Muting;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.MartialArts;

public partial class SharedMartialArtsSystem
{

    private void InitializeMimejutsu()
    {
        SubscribeLocalEvent<CanPerformComboComponent, MimejutsuSilentPunchPerformedEvent>(OnMimejutsuSilentPunch);
        SubscribeLocalEvent<CanPerformComboComponent, MimejutsuSilentExecutionPerformedEvent>(OnMimejutsuSilentExecution);
        SubscribeLocalEvent<CanPerformComboComponent, MimejutsuMimechucksPerformedEvent>(OnMimejutsuMimechucks);
        SubscribeLocalEvent<CanPerformComboComponent, MimejutsuSilencerPerformedEvent>(OnMimejutsuSilencer);
        SubscribeLocalEvent<CanPerformComboComponent, MimejutsuSilentPalmPerformedEvent>(OnMimejutsuSilentPalm);

        SubscribeLocalEvent<GrantMimejutsuComponent, UseInHandEvent>(OnGrantCQCUse);
    }

    private void OnMimejutsuAttackPerformed(Entity<MartialArtsKnowledgeComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (args.Type != ComboAttackType.Disarm || args.Weapon != args.Performer || args.Target == args.Performer)
            return;

        _stamina.TakeStaminaDamage(args.Target, 25f);
    }

    private void OnMimejutsuSilentPunch(Entity<CanPerformComboComponent> ent, ref MimejutsuSilentPunchPerformedEvent args)
    {
        if (_netManager.IsClient
            || !_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || !HasComp<StatusEffectsComponent>(target)
            || !_random.Prob(0.20f))
            return;

        _movementMod.TryUpdateMovementSpeedModDuration(target, MartsGenericSlow, TimeSpan.FromSeconds(2), 0.5f, 0.5f);
    }

    private void OnMimejutsuSilentExecution(Entity<CanPerformComboComponent> ent, ref MimejutsuSilentExecutionPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto) ||
            !TryUseMartialArt(ent, proto, out var target, out _) ||
            !TryComp(target, out BodyComponent? body) ||
            !TryComp(ent, out TargetingComponent? targeting))
            return;

        var (partType, symmetry) = _body.ConvertTargetBodyPart(targeting.Target);
        var targetedBodyPart = _body.GetBodyChildrenOfType(target, partType, body, symmetry).FirstOrNull();

        if (targetedBodyPart == null ||
            !TryComp(targetedBodyPart.Value.Id, out WoundableComponent? woundable) ||
            woundable.Bone.ContainedEntities.FirstOrNull() is not { } bone ||
            !TryComp(bone, out BoneComponent? boneComp))
            return;

        if (boneComp.BoneSeverity == BoneSeverity.Broken)
            DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        else
            _trauma.ApplyDamageToBone(bone, boneComp.BoneIntegrity, boneComp);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnMimejutsuMimechucks(Entity<CanPerformComboComponent> ent, ref MimejutsuMimechucksPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);
        ComboPopup(ent, target, proto.ID);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnMimejutsuSilencer(Entity<CanPerformComboComponent> ent, ref MimejutsuSilencerPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || !HasComp<StatusEffectsComponent>(target))
            return;

        _movementMod.TryUpdateMovementSpeedModDuration(target, MartsGenericSlow, TimeSpan.FromSeconds(3), 0.5f, 0.5f);

        _status.TryAddStatusEffect<MutedComponent>(target, "Muted", TimeSpan.FromSeconds(10), true);

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnMimejutsuSilentPalm(Entity<CanPerformComboComponent> ent, ref MimejutsuSilentPalmPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || !HasComp<StatusEffectsComponent>(target))
            return;

        var mapPos = Transform(ent).Coordinates.Position;
        var hitPos = Transform(target).Coordinates.Position;
        var dir = hitPos - mapPos;
        dir *= 4f / dir.Length();


        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _grabThrowing.Throw(target, ent, dir, proto.ThrownSpeed);
        _movementMod.TryUpdateMovementSpeedModDuration(target, MartsGenericSlow, TimeSpan.FromSeconds(1), 0.5f, 0.5f);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        ComboPopup(ent, target, proto.ID);
        ent.Comp.LastAttacks.Clear();
    }

}
