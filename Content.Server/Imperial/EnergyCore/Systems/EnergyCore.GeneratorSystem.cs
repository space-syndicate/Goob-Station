using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Events;
using Content.Server.Imperial.EnergyCore.Helpers;
using Content.Shared.Examine;

namespace Content.Server.Imperial.EnergyCore
{
    public sealed class CoreGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly CoreSearchSystem _coreHelper = default!;
        [Dependency] private readonly EnergyCoreSystem _core = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CoreGeneratorComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<CoreGeneratorComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CoreInitEvent>(OnNewCoreInit);
        }
        private void OnInit(EntityUid uid, CoreGeneratorComponent generator, ComponentInit args)
        {
            RejoinCore(uid, generator);
            generator.SearchTime = generator.SearchTime + _timing.CurTime;
        }
        private void OnExamined(EntityUid uid, CoreGeneratorComponent generator, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var nearestUid = generator.NearestCore;
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                args.PushMarkup(Loc.GetString("energycore-dont-any-near"));
                return;
            }
            var (coreTemp, tempRiseStatus) = GetCoreInfo(nearest);

            args.PushMarkup(Loc.GetString($"energycore-current-temp-change-{tempRiseStatus.ToString().ToLower()}"));

            var energyOutput = generator.EnergyOutput;
            args.PushMarkup(Loc.GetString("energycore-generator-current-energy-output", ("energyOutput", energyOutput)));
        }
        private void RejoinCore(EntityUid uid, CoreGeneratorComponent generator)
            => generator.NearestCore = _coreHelper.FindNearestEnergyCore(uid, _core.CoreHash, 10f);
        private void OnNewCoreInit(CoreInitEvent ev)
        {
            var query = EntityQueryEnumerator<CoreGeneratorComponent>();
            while (query.MoveNext(out var uid, out var generator))
            {
                RejoinCore(uid, generator);
            }
        }
        private void SetEnergyOutput(EntityUid uid, CoreGeneratorComponent generator, PowerSupplierComponent power)
        {
            var nearestUid = generator.NearestCore;

            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                generator.EnergyOutput = 0f;
                power.MaxSupply = 0f;
                return;
            }

            var (coreTemp, tempRiseStatus) = GetCoreInfo(nearest);

            if (coreTemp > 0f)
            {
                if (coreTemp > 500000f) // Ибо нехер забивать на ядро
                    generator.EnergyOutput = coreTemp / 10f;
                else
                    generator.EnergyOutput = coreTemp / generator.EnergyCoef;
                power.MaxSupply = generator.EnergyOutput;
            }
            else
            {
                generator.EnergyOutput = 0f;
                power.MaxSupply = 0f;
            }
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<CoreGeneratorComponent, PowerSupplierComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out var powr, out _))
            {
                SetEnergyOutput(uid, comp, powr);
            }
        }

        private (float coreTemp, CoreTempChangeLevel tempRiseStatus) GetCoreInfo(EnergyCoreComponent generator)
        {
            var coreTemp = generator.CoreTemp;
            var tempRiseStatus = generator.TempRiseStatus;

            return (coreTemp, tempRiseStatus);
        }
    }
}

