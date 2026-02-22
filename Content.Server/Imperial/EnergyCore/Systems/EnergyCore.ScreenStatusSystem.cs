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
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly CoreSearchSystem _coreHelper = default!;
        [Dependency] private readonly EnergyCoreSystem _core = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CoreStatusScreenComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<CoreStatusScreenComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CoreInitEvent>(OnNewCoreInit);
        }
        private void OnInit(EntityUid uid, CoreStatusScreenComponent screen, ComponentInit args)
        {
            RejoinCore(uid, screen);
            screen.SearchTime = screen.SearchTime + _timing.CurTime;
        }
        private void OnExamined(EntityUid uid, CoreStatusScreenComponent screen, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var nearestUid = screen.CheckedCore;
            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                return;
            }

            var (coreTemp, coreStatus, tempRiseStatus, safeProtocol, coreTempMult) = GetCoreInfo(nearest);

            args.PushMarkup(Loc.GetString($"energycore-current-temp-change-{tempRiseStatus.ToString().ToLower()}"));

            var protocol = Loc.GetString($"energycore-current-protocol-is-safe-{safeProtocol.ToString().ToLower()}");
            args.PushMarkup(protocol);

            var status = Loc.GetString($"energycore-screen-status-{coreStatus.ToString().ToLower()}");
            args.PushMarkup(status);

            args.PushMarkup(Loc.GetString("energycore-current-coef", ("coefficient", coreTempMult)));
            args.PushMarkup(Loc.GetString("energycore-current-temp", ("coreTemp", coreTemp)));
        }
        private void RejoinCore(EntityUid uid, CoreStatusScreenComponent screen)
            => screen.CheckedCore = _coreHelper.FindNearestEnergyCore(uid, _core.CoreHash, float.MaxValue);
        private void OnNewCoreInit(CoreInitEvent ev)
        {
            var query = EntityQueryEnumerator<CoreStatusScreenComponent>();
            while (query.MoveNext(out var uid, out var screen))
            {
                RejoinCore(uid, screen);
            }
        }
        private void UpdateScreenVisual(EntityUid uid, CoreStatusScreenComponent screen)
        {
            var nearestUid = screen.CheckedCore;

            if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
            {
                return;
            }
            var (coreTemp, coreStatus, tempRiseStatus, safeProtocol, coreTempMult) = GetCoreInfo(nearest);
            screen.SpriteStatus = (byte)coreStatus;
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, CoreStatusScreenVisual.Core_Screen_Visual, screen.SpriteStatus, appearance);
            }
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<CoreStatusScreenComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out _))
            {
                UpdateScreenVisual(uid, comp);
            }
        }
        private (float coreTemp, CoreStatus coreStatus, CoreTempChangeLevel tempRiseStatus, bool safeProtocol, float coreTempMult) GetCoreInfo(EnergyCoreComponent core)
        {
            var coreTemp = core.CoreTemp;
            var coreStatus = core.Status;
            var tempRiseStatus = core.TempRiseStatus;
            var safeProtocol = core.IsSafeProtocolActive;
            var coreTempMult = core.TempChangeMultiplier;

            return (coreTemp, coreStatus, tempRiseStatus, safeProtocol, coreTempMult);
        }
    }
}

