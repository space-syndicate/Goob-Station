using Content.Shared.Imperial.Damage;
using Content.Shared.Imperial.Damage.Events;
using Content.Shared.Damage;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.Damage
{
    public sealed class ImperialShieldSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ImperialShieldComponent, ImperialShieldRelayedEvent<DamageModifyEvent>>(OnUserDamageModified);
        }

        private void OnUserDamageModified(EntityUid uid, ImperialShieldComponent component, ref ImperialShieldRelayedEvent<DamageModifyEvent> args)
        {
            var modifier = component.PassiveBlockDamageModifer;
            Log.Debug($"if modifier == null start");
            if (modifier == null)
                return;
            Log.Debug($"modifier != null");
            args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifier);

            if (component.HasBlockSound)
                _audio.PlayPvs(component.BlockSound, uid);
        }
    }
}
