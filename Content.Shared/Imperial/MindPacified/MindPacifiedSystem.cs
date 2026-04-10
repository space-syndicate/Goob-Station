using Content.Shared.CombatMode.Pacification;
using Content.Shared.Imperial.MindPacified.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;

namespace Content.Shared.Imperial.MindPacified
{
    public sealed class MindPacifiedSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mind = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MindPacifiedComponent, MindGotAddedEvent>(OnMindGotAdded);
            SubscribeLocalEvent<GiveMindPacifiedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        }

        private void OnMindGotAdded(Entity<MindPacifiedComponent> ent, ref MindGotAddedEvent args)
        {
            EnsureComp<PacifiedComponent>(args.Container);
        }

        private void OnInteractionAttempt(Entity<GiveMindPacifiedComponent> ent, ref InteractionAttemptEvent args)
        {
            if (!_mind.TryGetMind(ent, out var mind, out _))
                return;

            EnsureComp<MindPacifiedComponent>(mind);
            RemCompDeferred<GiveMindPacifiedComponent>(ent);
        }
    }

}
