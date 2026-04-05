using Content.Shared.CombatMode.Pacification;
using Content.Shared.Imperial.MindPacified.Components;
using Content.Shared.Mind.Components;

namespace Content.Shared.Imperial.MindPacified
{
    public sealed class MindPacifiedSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MindPacifiedComponent, MindGotAddedEvent>(OnMindGotAdded);
        }

        private void OnMindGotAdded(Entity<MindPacifiedComponent> ent, ref MindGotAddedEvent args)
        {
            EnsureComp<PacifiedComponent>(args.Container);
        }
    }

}
