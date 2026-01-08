using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.Power.Components;
using Content.Server.Imperial.EnergyCore.Events;
using Content.Server.Imperial.EnergyCore.Helpers;
using Content.Shared.Examine;

namespace Content.Server.Imperial.EnergyCore
{
    public sealed class CoreStatusScreenSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly CoreSearchSystem _coreHelper = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CoreStatusScreenComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<CoreStatusScreenComponent, ComponentStartup>(OnStartup);
        }
        private void OnStartup(EntityUid uid, CoreStatusScreenComponent component, ComponentStartup args)
        {
            component.SearchTime = component.SearchTime + _timing.CurTime;
        }
        private void OnExamined(EntityUid uid, CoreStatusScreenComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var nearestUid = component.CheckedCore;
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                return;
            }

            var (coreTemp, coreStatus, tempRiseStatus, safeProtocol, coreTempMult) = GetCoreInfo(nearest);

            args.PushMarkup(Loc.GetString($"energycore-current-temp-change-{tempRiseStatus}"));

            var protocol = Loc.GetString($"energycore-current-protocol-is-safe-{safeProtocol.ToString().ToLower()}");
            args.PushMarkup(protocol);

            var status = Loc.GetString($"energycore-screen-status-{coreStatus.ToString().ToLower()}");
            args.PushMarkup(status);

            args.PushMarkup(Loc.GetString("energycore-current-coef", ("coefficient", coreTempMult)));
            args.PushMarkup(Loc.GetString("energycore-current-temp", ("coreTemp", coreTemp)));
        }
        private void UpdateScreenVisual(EntityUid uid, CoreStatusScreenComponent component)
        {
            var nearestUid = component.CheckedCore;

            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                return;
            }
            var (coreTemp, coreStatus, tempRiseStatus, safeProtocol, coreTempMult) = GetCoreInfo(nearest);
            component.SpriteStatus = (byte)coreStatus;
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, CoreStatusScreenVisual.Core_Screen_Visual, component.SpriteStatus, appearance);
            }
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<CoreStatusScreenComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out _))
            {
                UpdateScreenVisual(uid, comp);

                if (_timing.CurTime < comp.SearchTime) // Ищет только первые 5 секунд
                {
                    var nearestUid = _coreHelper.FindNearestEnergyCore(uid, float.MaxValue);
                    if (nearestUid == null ||
                        !EntityManager.TryGetComponent<EnergyCoreComponent>(nearestUid.Value, out var nearest))
                        continue;

                    comp.CheckedCore = nearestUid;
                }
            }
        }

        private (float coreTemp, CoreStatus coreStatus, byte tempRiseStatus, bool safeProtocol, float coreTempMult) GetCoreInfo(EnergyCoreComponent component)
        {
            var coreTemp = component.CoreTemp;
            var coreStatus = component.Status;
            var tempRiseStatus = component.TempChangeStatus;
            var safeProtocol = component.IsSafeProtocolActive;
            var coreTempMult = component.TempChangeMultiplier;

            return (coreTemp, coreStatus, tempRiseStatus, safeProtocol, coreTempMult);
        }
    }
}

