using Content.Shared.Imperial.MindPacified.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;

namespace Content.Shared.Imperial.MindPacified
{
    public sealed class GiveMindPacifiedSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mind = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GiveMindPacifiedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
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
