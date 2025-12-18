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
            var (coreTemp, tempRiseStatus) = GetCoreInfo(nearest);

            if (tempRiseStatus == 3)
                args.PushMarkup(Loc.GetString("energycore-current-temp-change-up"));
            if (tempRiseStatus == 2)
                args.PushMarkup(Loc.GetString("energycore-current-temp-change-auto"));
            if (tempRiseStatus == 1)
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
            var minDist = 10f;//float.MaxValue;

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
                generator.EnergyOutput = 0f;
                power.MaxSupply = 0f;
                return;
            }

            var (coreTemp, tempRiseStatus) = GetCoreInfo(nearest);
            if (HasComp<PowerSupplierComponent>(uid))
            {
                if (coreTemp > 0f || nearestUid != null)
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
            else generator.EnergyOutput = 0f;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<CoreGeneratorComponent, PowerSupplierComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out var powr, out _))
            {
                SetEnergyOutput(uid, comp, powr);
                var nearestUid = FindNearestEnergyCore(uid);
                if (nearestUid == null ||
                    !EntityManager.TryGetComponent<EnergyCoreComponent>(nearestUid.Value, out var nearest))
                    continue;

                var (coreTemp, tempRiseStatus) = GetCoreInfo(nearest);
            }
        }

        private static (float coreTemp, byte tempRiseStatus) GetCoreInfo(EnergyCoreComponent component)
        {
            var coreTemp = component.CoreTemp;
            var tempRiseStatus = component.TempChangeStatus;

            return (coreTemp, tempRiseStatus);
        }
    }
}

