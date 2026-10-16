using Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;
using Content.Shared.EntityEffects;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Effects;

public sealed partial class RechargeBatteryEffectSystem : EntityEffectSystem<BatteryComponent, RechargeBattery>
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    protected override void Effect(Entity<BatteryComponent> entity, ref EntityEffectEvent<RechargeBattery> args)
    {
        _battery.ChangeCharge((entity, entity.Comp), entity.Comp.MaxCharge * args.Effect.Fraction * args.Scale);
    }
}
