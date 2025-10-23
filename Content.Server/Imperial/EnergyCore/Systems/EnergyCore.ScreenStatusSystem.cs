using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.Power.Components;
using Content.Shared.Examine;
using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.EnergyCore
{
    public sealed class CoreStatusScreenSystem : EntitySystem
    {
        // Поиск ближайшего ядра был основан на консоли СМ
        // Спасибо тебе, xxRay, без тебя и твоей суперматерии я бы это никогда не написал
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CoreStatusScreenComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, CoreStatusScreenComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var nearestUid = FindNearestEnergyCore(uid);
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                args.PushMarkup(Loc.GetString("energycore-dont-any-near"));
                return;
            }
            var (coreTemp, coreStatus, isHeatUp, safeProtocol, coreTempMult) = GetCoreInfo(nearest);

            if (isHeatUp)
                args.PushMarkup(Loc.GetString("energycore-current-temp-change-up"));
            else
                args.PushMarkup(Loc.GetString("energycore-current-temp-change-down"));

            if (safeProtocol)
                args.PushMarkup(Loc.GetString("energycore-current-protocol-on"));
            else
                args.PushMarkup(Loc.GetString("energycore-current-protocol-off"));

            if ((byte)coreStatus == 1)
                args.PushMarkup(Loc.GetString("energycore-status-offline"));
            if ((byte)coreStatus == 2)
                args.PushMarkup(Loc.GetString("energycore-status-idle"));
            if ((byte)coreStatus == 3)
                args.PushMarkup(Loc.GetString("energycore-status-stable"));
            if ((byte)coreStatus == 4)
                args.PushMarkup(Loc.GetString("energycore-status-optimal"));
            if ((byte)coreStatus == 5)
                args.PushMarkup(Loc.GetString("energycore-status-moderate"));
            if ((byte)coreStatus == 6)
                args.PushMarkup(Loc.GetString("energycore-status-high"));
            if ((byte)coreStatus == 7)
                args.PushMarkup(Loc.GetString("energycore-status-criticalhigh"));
            if ((byte)coreStatus == 8)
                args.PushMarkup(Loc.GetString("energycore-status-catastrophic"));
            if ((byte)coreStatus == 9)
                args.PushMarkup(Loc.GetString("energycore-status-safeprotocol-active"));

            args.PushMarkup(Loc.GetString("energycore-current-coef", ("coefficient", coreTempMult)));
            args.PushMarkup(Loc.GetString("energycore-current-temp", ("coreTemp", coreTemp)));
        }
        private void UpdateScreenVisual(EntityUid uid, CoreStatusScreenComponent component)
        {
            var nearestUid = FindNearestEnergyCore(uid);
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                return;
            }
            var (coreTemp, coreStatus, isHeatUp, safeProtocol, coreTempMult) = GetCoreInfo(nearest);
            component.ScreenStatus = (byte)coreStatus;
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, CoreStatusScreenVisual.Core_Screen_Visual, component.ScreenStatus, appearance);
            }
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<CoreStatusScreenComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out _))
            {
                var nearestUid = FindNearestEnergyCore(uid);
                if (nearestUid == null ||
                    !EntityManager.TryGetComponent<EnergyCoreComponent>(nearestUid.Value, out var nearest))
                    continue;

                var (coreTemp, coreStatus, isHeatUp, safeProtocol, coreTempMult) = GetCoreInfo(nearest);
                UpdateScreenVisual(uid, comp);
            }
        }

        private static (float coreTemp, CoreStatus coreStatus, bool isHeatUp, bool safeProtocol, float coreTempMult) GetCoreInfo(EnergyCoreComponent component)
        {                               //boolTempChangeMultiplier
            var coreTemp = component.CoreTemp;
            var coreStatus = component.Status;
            var isHeatUp = component.CoreTempRise;
            var safeProtocol = component.IsSafeProtocolActive;
            var coreTempMult = component.TempChangeMultiplier;

            return (coreTemp, coreStatus, isHeatUp, safeProtocol, coreTempMult);
        }
    }
}

