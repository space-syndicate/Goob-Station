using System.Linq;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Shared.Power;
using Content.Shared.Access.Components;
using Content.Shared.Imperial.Crook.Visuals;
using Content.Server.Imperial.Crook.Components;
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.Contraband;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Physics.Events;
using Content.Shared.Stunnable;
using Content.Shared.Damage;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Server.Containers;

namespace Content.Server.Imperial.Crook.Systems
{
    public sealed class MetalDetectorSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedIdCardSystem _idCard = default!;
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly ContainerSystem _container = default!;

        private const float ThinkRate = 0.25f;
        private float _accumulatedTime;
        private readonly HashSet<EntityUid> _shockedEntities = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MetalDetectorComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<MetalDetectorComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<MetalDetectorComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<MetalDetectorComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<MetalDetectorComponent, EndCollideEvent>(OnEndCollide);
        }

        private void OnStartCollide(EntityUid uid, MetalDetectorComponent comp, ref StartCollideEvent args)
        {
            if (!comp.Powered)
                return;

            var otherEntity = args.OtherEntity;

            if (!HasComp<ItemComponent>(otherEntity) && !HasComp<InventoryComponent>(otherEntity))
                return;

            if (comp.CollidingEntities.Add(otherEntity))
                ProcessEntity(uid, otherEntity, comp);
        }

        private void OnEndCollide(EntityUid uid, MetalDetectorComponent comp, ref EndCollideEvent args)
        {
            comp.CollidingEntities.Remove(args.OtherEntity);

            if (comp.CollidingEntities.Count == 0)
                ResetToDefaultState(uid, comp);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulatedTime += frameTime;
            if (_accumulatedTime < ThinkRate)
                return;

            _accumulatedTime -= ThinkRate;
            _shockedEntities.Clear();

            var query = EntityQueryEnumerator<MetalDetectorComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (!comp.Powered)
                {
                    if (comp.State != MetalDetectorVisualState.Off)
                        SetState(uid, MetalDetectorVisualState.Off, comp);
                    continue;
                }

                if (comp.CollidingEntities.Count > 0 && _timing.CurTime > comp.NextStateReset)
                    ResetToDefaultState(uid, comp);
            }
        }

        private void ProcessEntity(EntityUid detector, EntityUid entity, MetalDetectorComponent comp)
        {
            if (!comp.Powered)
                return;

            comp.NextStateReset = _timing.CurTime + comp.StateResetDelay;
            bool hasContraband = CheckForContraband(detector, entity, comp);
            bool hasAccess = HasRequiredAccess(entity, comp);
            HandleDetectionResults(detector, entity, comp, hasContraband, hasAccess);
        }

        private bool CheckForContraband(EntityUid detector, EntityUid target, MetalDetectorComponent comp)
        {
            if (IsContrabandItem(detector, target, comp) ||
                CheckEntityAndContainers(detector, target, comp))
            {
                return true;
            }

            if (TryComp<InventoryComponent>(target, out _))
            {
                foreach (var slot in comp.CheckedSlots)
                {
                    if (_inventory.TryGetSlotEntity(target, slot, out var item) &&
                        (IsContrabandItem(detector, item.Value, comp) ||
                         CheckEntityAndContainers(detector, item.Value, comp)))
                    {
                        return true;
                    }
                }
            }

            foreach (var held in _hands.EnumerateHeld(target))
            {
                if (IsContrabandItem(detector, held, comp) ||
                    CheckEntityAndContainers(detector, held, comp))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckEntityAndContainers(EntityUid detector, EntityUid entity, MetalDetectorComponent comp, int currentDepth = 0)
        {
            if (currentDepth > comp.MaxRecursionDepth || IsIgnoredByDetector(detector, entity, comp))
                return false;

            if (IsContrabandItem(detector, entity, comp))
                return true;

            if (TryComp<ContainerManagerComponent>(entity, out var containerManager))
            {
                foreach (var container in containerManager.Containers.Values)
                {
                    foreach (var contained in container.ContainedEntities)
                    {
                        if (CheckEntityAndContainers(detector, contained, comp, currentDepth + 1))
                            return true;
                    }
                }
            }

            return false;
        }

        private bool IsIgnoredByDetector(EntityUid detector, EntityUid entity, MetalDetectorComponent comp)
        {
            var meta = MetaData(entity);
            var prototypeId = meta.EntityPrototype?.ID;

            if (!string.IsNullOrEmpty(prototypeId) && comp.IgnoredPrototypes.Contains(prototypeId))
                return true;

            var current = entity;
            while (_container.TryGetContainingContainer(current, out var container))
            {
                current = container.Owner;
                var currentMeta = MetaData(current);
                var currentPrototypeId = currentMeta.EntityPrototype?.ID;

                if (!string.IsNullOrEmpty(currentPrototypeId) && comp.IgnoredPrototypes.Contains(currentPrototypeId))
                    return true;
            }

            return false;
        }

        private bool IsContrabandItem(EntityUid detector, EntityUid item, MetalDetectorComponent comp)
        {
            if (IsIgnoredByDetector(detector, item, comp))
                return false;

            return TryComp<ContrabandComponent>(item, out _) &&
                   !IsContrabandAllowed(detector, item, comp);
        }

        private bool IsContrabandAllowed(EntityUid detector, EntityUid contrabandItem, MetalDetectorComponent detectorComp)
        {
            var current = contrabandItem;
            while (_container.TryGetContainingContainer(current, out var container))
            {
                current = container.Owner;
                if (HasRequiredAccess(current, detectorComp))
                    return true;
            }

            return false;
        }

        private void HandleDetectionResults(EntityUid detector, EntityUid entity,
         MetalDetectorComponent comp,
         bool hasContraband, bool hasAccess)
        {
            if (!comp.Powered)
                return;

            if (HasComp<EmaggedComponent>(detector))
            {
                if (!_shockedEntities.Contains(entity) && TryComp<DamageableComponent>(entity, out _))
                {
                    _shockedEntities.Add(entity);
                    _damageable.TryChangeDamage(entity, comp.ShockDamage);
                    _stun.TryParalyze(entity, comp.ShockDuration, true);
                    _audio.PlayPvs(comp.ShockSound, detector);
                    DischargeBatteries(entity, comp);

                    _popup.PopupEntity(
                        Loc.GetString("metal-detector-electrocuted"),
                        entity,
                        PopupType.SmallCaution);
                }
                SetStateWithSound(detector, MetalDetectorVisualState.Alert, comp, comp.AlertSound);
                return;
            }

            if (hasAccess)
            {
                SetStateWithSound(detector, MetalDetectorVisualState.Scanning, comp, comp.ClearSound);
            }
            else if (hasContraband && comp.CheckContraband)
            {
                SetStateWithSound(detector, MetalDetectorVisualState.Alert, comp, comp.AlertSound);
            }
            else
            {
                SetStateWithSound(detector, MetalDetectorVisualState.Scanning, comp, comp.ClearSound);
            }
        }

        private void SetState(EntityUid uid, MetalDetectorVisualState state, MetalDetectorComponent comp)
        {
            if (comp.State == state)
                return;

            comp.State = state;
            _appearance.SetData(uid, MetalDetectorVisuals.State, state);
        }

        private void SetStateWithSound(EntityUid uid, MetalDetectorVisualState state,
                                     MetalDetectorComponent comp, SoundSpecifier sound)
        {
            SetState(uid, state, comp);
            _audio.PlayPvs(sound, uid);
        }

        private void ResetToDefaultState(EntityUid uid, MetalDetectorComponent comp)
        {
            if (!comp.Powered)
            {
                SetState(uid, MetalDetectorVisualState.Off, comp);
                return;
            }

            SetState(uid, MetalDetectorVisualState.Powered, comp);
        }

        private void OnPowerChanged(EntityUid uid, MetalDetectorComponent comp, ref PowerChangedEvent args)
        {
            comp.Powered = args.Powered;

            if (comp.Powered)
            {
                ResetToDefaultState(uid, comp);
            }
            else
            {
                SetState(uid, MetalDetectorVisualState.Off, comp);
                comp.CollidingEntities.Clear();
                _shockedEntities.Clear();
            }
        }

        private void OnStartup(Entity<MetalDetectorComponent> detector, ref ComponentStartup args)
        {
            ResetToDefaultState(detector.Owner, detector.Comp);
        }

        private void OnEmagged(EntityUid uid, MetalDetectorComponent comp, ref GotEmaggedEvent args)
        {
            if (HasComp<EmaggedComponent>(uid))
                return;

            EnsureComp<EmaggedComponent>(uid);
            args.Handled = true;
            SetStateWithSound(uid, MetalDetectorVisualState.Alert, comp, comp.AlertSound);
        }

        private void DischargeBatteries(EntityUid entity, MetalDetectorComponent detectorComp)
        {
            if (TryComp<InventoryComponent>(entity, out var inventory))
            {
                foreach (var slot in detectorComp.CheckedSlots)
                {
                    if (_inventory.TryGetSlotEntity(entity, slot, out var item))
                        DischargeSingleItem(item.Value);
                }
            }

            foreach (var heldItem in _hands.EnumerateHeld(entity))
                DischargeSingleItem(heldItem);
        }

        private void DischargeSingleItem(EntityUid item)
        {
            if (TryComp<BatteryComponent>(item, out var battery))
                _battery.SetCharge(item, 0, battery);

            if (TryComp<ContainerManagerComponent>(item, out var containerManager))
            {
                foreach (var container in containerManager.Containers.Values)
                {
                    foreach (var containedItem in container.ContainedEntities)
                    {
                        DischargeSingleItem(containedItem);
                    }
                }
            }
        }

        private bool HasRequiredAccess(EntityUid userUid, MetalDetectorComponent detector)
        {
            if (TryComp<AccessComponent>(userUid, out var access) &&
                detector.AllowedAccess.Any(required => access.Tags.Contains(required)))
            {
                return true;
            }

            return _idCard.TryFindIdCard(userUid, out var idCard) &&
                   TryComp<AccessComponent>(idCard, out var idAccess) &&
                   detector.AllowedAccess.Any(required => idAccess.Tags.Contains(required));
        }
    }
}
