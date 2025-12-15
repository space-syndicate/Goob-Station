using Content.Shared.EntityEffects;
using Content.Shared.Interaction;
using Robust.Server.Audio;

namespace Content.Server.Imperial.Medieval.Magic.MedievalSpellOnInteract;


public sealed partial class MedievalSpellOnInteractSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalSpellOnInteractComponent, AfterInteractEvent>(OnInteract);
    }


    public void OnInteract(EntityUid uid, MedievalSpellOnInteractComponent component, AfterInteractEvent args)
    {
        if (args.Target == null) return;

        component.UseCount += 1;

        var effects = args.User == args.Target
            ? component.SelfUseEffects.Length == 0
                ? component.Effects
                : component.SelfUseEffects
            : component.Effects;

        _entityEffectsSystem.ApplyEffects(args.Target.Value, effects, user: args.User);
        _audioSystem.PlayPvs(component.SoundOnInteract, args.User);

        if (component.UseCount >= component.RemainingUses)
            QueueDel(uid);

        args.Handled = true;
    }
}
