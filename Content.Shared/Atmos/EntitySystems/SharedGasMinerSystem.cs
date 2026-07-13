// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Temperature;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasMinerSystem : EntitySystem
{
    [Dependency] private readonly SharedAtmosphereSystem _sharedAtmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasMinerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<GasMinerComponent> ent, ref ExaminedEvent args)
    {
        var component = ent.Comp;

        using (args.PushGroup(nameof(GasMinerComponent)))
        {
            // CorvaxGoob-GasMiners-Start
            if (component.ListSpawnGas is not null)
            {
                string gasesFormat = "";
                int len = component.ListSpawnGas.Count;
                int i = 0;

                foreach (var gas in component.ListSpawnGas)
                {
                    gasesFormat += Loc.GetString(_sharedAtmosphereSystem.GetGas(gas.Key).Name)
                        + $" ({Math.Round(gas.Value * 100, 2)}%)"
                        + ((i < len - 1) ? ", " : "");
                    i++;
                }

                args.PushMarkup(Loc.GetString("gas-miner-mines-text", ("gas", gasesFormat)));
            }

            if (component.SpawnGas is not null) // CorvaxGoob-GasMiners-End
                args.PushMarkup(Loc.GetString("gas-miner-mines-text",
                    ("gas", Loc.GetString(_sharedAtmosphereSystem.GetGas(component.SpawnGas.Value).Name))));

            args.PushText(Loc.GetString("gas-miner-amount-text",
                ("moles", $"{component.SpawnAmount:0.#}")));

            args.PushText(Loc.GetString("gas-miner-temperature-text",
                ("tempK", $"{component.SpawnTemperature:0.#}"),
                ("tempC", $"{TemperatureHelpers.KelvinToCelsius(component.SpawnTemperature):0.#}")));

            if (component.MaxExternalAmount < float.PositiveInfinity)
            {
                args.PushText(Loc.GetString("gas-miner-moles-cutoff-text",
                    ("moles", $"{component.MaxExternalAmount:0.#}")));
            }

            if (component.MaxExternalPressure < float.PositiveInfinity)
            {
                args.PushText(Loc.GetString("gas-miner-pressure-cutoff-text",
                    ("pressure", $"{component.MaxExternalPressure:0.#}")));
            }

            args.AddMarkup(component.MinerState switch
            {
                GasMinerState.Disabled => Loc.GetString("gas-miner-state-disabled-text"),
                GasMinerState.Idle => Loc.GetString("gas-miner-state-idle-text"),
                GasMinerState.Working => Loc.GetString("gas-miner-state-working-text"),
                // C# pattern matching is not exhaustive for enums
                _ => throw new IndexOutOfRangeException(nameof(component.MinerState)),
            });
        }
    }
}
