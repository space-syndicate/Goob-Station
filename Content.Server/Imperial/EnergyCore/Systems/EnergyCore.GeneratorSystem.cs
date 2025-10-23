using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Shared.Examine;
using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.EnergyCore
{
    public sealed class CoreGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CoreGeneratorComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, CoreGeneratorComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var nearestUid = FindNearestEnergyCore(uid);
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                args.PushMarkup(Loc.GetString("energycore-dont-any-near"));
                return;
            }
            var (coreTemp, isHeatUp) = GetCoreInfo(nearest);

            if (isHeatUp == true)
                args.PushMarkup(Loc.GetString("energycore-current-temp-change-up"));
            else
                args.PushMarkup(Loc.GetString("energycore-current-temp-change-down"));

            var energyOutput = component.EnergyOutput;
            args.PushMarkup(Loc.GetString("energycore-generator-current-energy-output", ("energyOutput", energyOutput)));
        }
        private EntityUid? FindNearestEnergyCore(EntityUid core)
        {
            var transformCompConsole = Transform(core);
            var mapId = transformCompConsole.MapID;
            var pos = _transformSystem.GetMapCoordinates(transformCompConsole).Position;

            EntityUid? nearest = null;
            var minDist = float.MaxValue;

            var enumerator = EntityQueryEnumerator<EnergyCoreComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out _, out var transComp))
            {
                if (transComp.MapID != mapId)
                    continue;

                var corepos = _transformSystem.GetMapCoordinates(uid).Position;
                var dist = (corepos - pos).LengthSquared();
                if (dist > minDist)
                    continue;

                minDist = dist;
                nearest = uid;
            }
            return nearest;
        }
        private void SetEnergyOutput(EntityUid uid, CoreGeneratorComponent generator, PowerSupplierComponent power)
        {
            var nearestUid = FindNearestEnergyCore(uid);
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                return;
            }
            var (coreTemp, isHeatUp) = GetCoreInfo(nearest);
            if (HasComp<PowerSupplierComponent>(uid))
            {
                if (coreTemp > 0f)
                {
                    if (coreTemp > 500000f) // Ибо нехер забивать на ядро
                        generator.EnergyOutput = coreTemp / 10f;
                    else
                        generator.EnergyOutput = coreTemp / generator.EnergyCoef;
                    power.MaxSupply = generator.EnergyOutput;
                }
                else
                    return;
            }
            else
                return;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<CoreGeneratorComponent, PowerSupplierComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out var powr, out _))
            {
                var nearestUid = FindNearestEnergyCore(uid);
                if (nearestUid == null ||
                    !EntityManager.TryGetComponent<EnergyCoreComponent>(nearestUid.Value, out var nearest))
                    continue;

                var (coreTemp, isHeatUp) = GetCoreInfo(nearest);
                SetEnergyOutput(uid, comp, powr);
            }
        }

        private static (float coreTemp, bool isHeatUp) GetCoreInfo(EnergyCoreComponent component)
        {
            var coreTemp = component.CoreTemp;
            var isHeatUp = component.CoreTempRise;

            return (coreTemp, isHeatUp);
        }
    }
}

