using Content.Goobstation.Shared.ModSuits.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.ModSuits.Modules;

public sealed partial class ModThermalRegulatorModuleSystem : EntitySystem
{
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;

    private TimeSpan _lastUpdate;
    private TimeSpan _nextUpdate;
    private bool _hasUpdated;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        var elapsed = _hasUpdated
            ? MathF.Min((float) (now - _lastUpdate).TotalSeconds, 0.5f)
            : frameTime;

        _hasUpdated = true;
        _lastUpdate = now;
        _nextUpdate = now + TimeSpan.FromSeconds(0.1);

        var query = EntityQueryEnumerator<ModThermalRegulatorModuleComponent, ModModuleComponent>();
        while (query.MoveNext(out _, out var regulator, out var module))
        {
            if (!module.Active
                || !TryComp<ModSuitComponent>(module.Suit, out var suit)
                || suit.Wearer is not { } wearer
                || !TryComp<TemperatureComponent>(wearer, out var temperature))
                continue;

            var difference = regulator.TargetTemperature - temperature.CurrentTemperature;
            if (MathF.Abs(difference) < 0.01f)
                continue;

            var step = MathF.Sign(difference) * MathF.Min(MathF.Abs(difference), regulator.Rate * elapsed);
            var heat = step * _temperature.GetHeatCapacity(wearer, temperature);
            _temperature.ChangeHeat(wearer, heat, true, temperature);
        }
    }
}
