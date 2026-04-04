using Content.Shared.CombatMode.Pacification;
using Content.Shared.Imperial.AutoPacified.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Shared.Imperial.AutoPacified;
public sealed class AutoPacifiedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindGotAddedEvent>(OnMindAdded);
    }

    private void OnMindAdded(MindGotAddedEvent args)
    {
        if (args.TransferEntity == null)
            return;

        if (HasComp<AutoPacifiedComponent>(args.Mind))
        {
            EnsureComp<PacifiedComponent>(args.TransferEntity.Value);
        }
    }
}
