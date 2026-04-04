using Content.Shared.Mind.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Imperial.AutoPacified.Components;

namespace Content.Shared.Imperial.AutoPacified;
public sealed class AutoPacifiedSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(MindAddedMessage args)
    {
        if (HasComp<AutoPacifiedComponent>(args.Mind))
        {
            EnsureComp<PacifiedComponent>(args.Container);
        }
    }
}
