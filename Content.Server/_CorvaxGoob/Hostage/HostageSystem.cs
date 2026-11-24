using Content.Goobstation.Shared.Sprinting;
using Content.Server._CorvaxGoob.Surrender;
using Content.Server.Camera;
using Content.Server.Chat;
using Content.Server.CombatMode;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Weapons.Melee;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._CorvaxGoob.Appearance;
using Content.Shared._CorvaxGoob.Hostage;
using Content.Shared._CorvaxGoob.Surrender;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Execution;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace Content.Server._CorvaxGoob.Hostage;

public sealed partial class HostageSystem : EntitySystem
{
    [Dependency] private SurrenderSystem _surrender = default!;
    [Dependency] private SharedHostageSystem _hostage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private CameraRecoilSystem _recoil = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private CombatModeSystem _combat = default!;
    [Dependency] private SharedSuicideSystem _suicide = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private MeleeWeaponSystem _melee = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private TimeSpan _updateInterval = TimeSpan.FromSeconds(0.25);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, StartTakeHostageDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HostageTakerComponent, ExecuteHostageAlertEvent>(OnExecuteHostageAlert);
        SubscribeLocalEvent<HostageTakerComponent, FreeHostageAlertEvent>(OnFreeHostageAlert);

        SubscribeLocalEvent<HostageTakerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HostageTakerComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<HostageComponent, TryStopSurrendingEvent>(OnTryStopSurrending);

        SubscribeLocalEvent<HostageTakerComponent, MobStateChangedEvent>(OnHostagerMobStateChanged);
        SubscribeLocalEvent<HostageComponent, MobStateChangedEvent>(OnHostageMobStateChanged);
        SubscribeLocalEvent<HostageTakerComponent, AutoKillAlertEvent>(OnAutoKillToggleAlert);
    }
    private void OnAutoKillToggleAlert(Entity<HostageTakerComponent> entity, ref AutoKillAlertEvent ev)
    {
        entity.Comp.AutoKill = !entity.Comp.AutoKill;
        _alerts.UpdateAlert(entity.Owner, entity.Comp.ToggleKillAlert, (short) (entity.Comp.AutoKill ? 0 : 1));
    }

    private void OnHostageMobStateChanged(Entity<HostageComponent> entity, ref MobStateChangedEvent args) =>
        StopHostaging(entity.Comp.Taker, entity);

    private void OnHostagerMobStateChanged(Entity<HostageTakerComponent> entity, ref MobStateChangedEvent args) =>
        StopHostaging(entity, entity.Comp.Hostage);

    private void OnTryStopSurrending(Entity<HostageComponent> ent, ref TryStopSurrendingEvent args)
    {
        args.Cancelled = true;
    }

    private void OnComponentInit(Entity<HostageTakerComponent> ent, ref ComponentInit args)
    {
        if (_proto.TryIndex<AlertPrototype>(ent.Comp.FreeAlert, out var freeAlert))
            _alerts.ShowAlert(ent.Owner, freeAlert);

        if (_proto.TryIndex<AlertPrototype>(ent.Comp.ExecuteAlert, out var executeAlert))
            _alerts.ShowAlert(ent.Owner, executeAlert);

        if (_proto.TryIndex<AlertPrototype>(ent.Comp.ToggleKillAlert, out var autoKillAlert))
            _alerts.ShowAlert(ent.Owner, autoKillAlert, 0);

        _modifier.RefreshMovementSpeedModifiers(ent);

        var effect = SpawnAtPosition(ent.Comp.HostageTakerEffect, Transform(ent).Coordinates);
        _xform.SetParent(effect, ent);

        

        ent.Comp.HostageTakerEffectUid = effect;
    }

    private void OnComponentShutdown(Entity<HostageTakerComponent> ent, ref ComponentShutdown args)
    {
        if (_proto.TryIndex<AlertPrototype>(ent.Comp.ExecuteAlert, out var executeAlert))
            _alerts.ClearAlert(ent.Owner, executeAlert);

        if (_proto.TryIndex<AlertPrototype>(ent.Comp.FreeAlert, out var freeAlert))
            _alerts.ClearAlert(ent.Owner, freeAlert);

        if (_proto.TryIndex<AlertPrototype>(ent.Comp.ToggleKillAlert, out var autoKillAlert))
            _alerts.ClearAlert(ent.Owner, autoKillAlert);

        if (TryComp<SurrenderComponent>(ent.Comp.Hostage, out var surrender) && TryComp<AppearanceComponent>(surrender.SurrenderEffect, out var surrenderEffectAppearance))
        {
            _appearance.SetData(surrender.SurrenderEffect.Value, SurrendVisualType.IsHostaged, false, surrenderEffectAppearance);
            Dirty(surrender.SurrenderEffect.Value, surrenderEffectAppearance);
        }

        QueueDel(ent.Comp.HostageTakerEffectUid);
    }

    private void OnExecuteHostageAlert(Entity<HostageTakerComponent> entity, ref ExecuteHostageAlertEvent ev) => TryExecute(entity, ignoreMinDistance: true);

    private void OnFreeHostageAlert(Entity<HostageTakerComponent> entity, ref FreeHostageAlertEvent ev)
    {
        StopHostaging(entity, entity.Comp.Hostage);
    }

    private void OnDoAfter(Entity<MobStateComponent> entity, ref StartTakeHostageDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (ev.Target is null)
            return;

        if (!_hostage.CanHostage(ev.Args.User, ev.Target.Value, ev.Gun))
            return;

        var surrender = EnsureComp<SurrenderComponent>(ev.Target.Value);

        var hostageTaker = EnsureComp<HostageTakerComponent>(ev.User);
        var hostage = EnsureComp<HostageComponent>(ev.Target.Value);

        hostageTaker.Hostage = ev.Target.Value;
        hostageTaker.Weapon = ev.Gun;

        hostage.Taker = ev.User;
        hostage.Gun = ev.Gun;

        if (hostageTaker.HostageTakerEffectUid is not null && TryComp<AppearanceComponent>(hostageTaker.HostageTakerEffectUid, out var appearance))
        {
            _appearance.SetData(hostageTaker.HostageTakerEffectUid.Value, CopySpriteAppearance.NetEntity, GetNetEntity(ev.Gun));
            _appearance.SetData(hostageTaker.HostageTakerEffectUid.Value, CopySpriteAppearance.NoRot, true);

        }

        if (TryComp<AppearanceComponent>(surrender.SurrenderEffect, out var surrenderEffectAppearance))
        {
            _appearance.SetData(surrender.SurrenderEffect.Value, SurrendVisualType.IsHostaged, true, surrenderEffectAppearance);
            Dirty(surrender.SurrenderEffect.Value, surrenderEffectAppearance);
        }

        Dirty(ev.User, hostageTaker);
        Dirty(ev.Target.Value, hostage);

        _pulling.TryStartPull(ev.User, entity, grabStageOverride: Goobstation.Common.Grab.GrabStage.Hard);

        _popup.PopupEntity(Loc.GetString("hostage-popup-initial-external", ("attacker", Identity.Entity(entity, EntityManager)), ("weapon", ev.Gun), ("victim", Identity.Entity(hostageTaker.Hostage, EntityManager))), entity, Shared.Popups.PopupType.LargeCaution);

        _popup.PopupClient(Loc.GetString("hostage-popup-initial-hostage"), entity, hostageTaker.Hostage, Shared.Popups.PopupType.LargeCaution);
        _popup.PopupClient(Loc.GetString("hostage-popup-initial-hostage-taker"), hostageTaker.Hostage, entity, Shared.Popups.PopupType.LargeCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + _updateInterval;

        var query = AllEntityQuery<HostageTakerComponent>();
        while (query.MoveNext(out var uid, out var hostageTaker))
            TryExecute((uid, hostageTaker), hostageTaker.AutoKill);
    }

    private bool TryExecute(Entity<HostageTakerComponent> entity, bool tryKill = true, bool ignoreMinDistance = false)
    {
        var pos = _xform.GetWorldPosition(entity);
        var posHostage = _xform.GetWorldPosition(entity.Comp.Hostage);

        var distance = (pos - posHostage).Length();

        if (!ignoreMinDistance && distance < entity.Comp.MinKillDistance)
            return false;

        if (distance <= entity.Comp.MaxKillDistance
            && tryKill
            && TryComp<HandsComponent>(entity, out var hands)
            && _hands.IsHolding((entity.Owner, hands), entity.Comp.Weapon))
        {
            // Get the direction for the recoil
            Vector2 direction = Vector2.Zero;
            var attackerXform = Transform(entity);
            var victimXform = Transform(entity.Comp.Hostage);
            var diff = victimXform.WorldPosition - attackerXform.WorldPosition;
            if (diff != Vector2.Zero)
                direction = -diff.Normalized(); // recoil opposite of shot


            if (!_hostage.CanHostage(entity, entity.Comp.Hostage, entity.Comp.Weapon)
                || !TryComp<DamageableComponent>(entity.Comp.Hostage, out var damageableComponent))
                return false;

            if (TryComp<GunComponent>(entity.Comp.Weapon, out var gunComp))
            {
                // Take some ammunition for the shot (one bullet)
                var fromCoordinates = Transform(entity).Coordinates;
                var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, entity);
                RaiseLocalEvent(entity.Comp.Weapon, ev);

                // Check if there's any ammo left
                if (ev.Ammo.Count <= 0)
                {
                    _audio.PlayPvs(gunComp.SoundEmpty, entity.Comp.Weapon);
                    return false;
                }

                DamageSpecifier damage = new DamageSpecifier();
                string? mainDamageType = null;
                // Get some information from IShootable
                var ammoUid = ev.Ammo[0].Entity;

                switch (ev.Ammo[0].Shootable)
                {
                    case CartridgeAmmoComponent cartridge:
                        {
                            if (cartridge.Spent)
                            {
                                _audio.PlayPvs(gunComp.SoundEmpty, entity);
                                return false;
                            }

                            var prototype = _proto.Index<EntityPrototype>(cartridge.Prototype);

                            prototype.TryGetComponent<ProjectileComponent>(out var projectileA, _factory); // sloth forgive me

                            if (projectileA != null)
                            {
                                damage = projectileA.Damage;
                                mainDamageType = GetDamage(damage, mainDamageType);
                            }

                            cartridge.Spent = true; // Expend the cartridge
                            _appearance.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                            Dirty(ammoUid.Value, cartridge);

                            break;
                        }
                    case AmmoComponent newAmmo: // This stops revolvers from hitting the user while executing someone, somehow
                        TryComp<ProjectileComponent>(ammoUid, out var projectileB);

                        if (projectileB != null)
                        {
                            damage = projectileB.Damage;
                            mainDamageType = GetDamage(damage, mainDamageType);
                        }

                        if (ammoUid != null)
                            PredictedDel(ammoUid.Value); // Trauma - ammo spawning is predicted
                        break;
                }

                if (HasComp<BatteryAmmoProviderComponent>(entity.Comp.Weapon)) // Almost all hitscans are heat so this should work fine
                    mainDamageType = "Heat";

                var prev = _combat.IsInCombatMode(entity);
                _combat.SetInCombatMode(entity, true);

                _recoil.KickCamera(entity, direction);

                _popup.PopupEntity(Loc.GetString("execution-popup-gun-complete-external", ("attacker", Identity.Entity(entity, EntityManager)), ("victim", Identity.Entity(entity.Comp.Hostage, EntityManager))), entity, Shared.Popups.PopupType.LargeCaution);

                _audio.PlayPvs(gunComp.SoundGunshot, entity.Comp.Weapon);
                _suicide.ApplyLethalDamage((entity.Comp.Hostage, damageableComponent), mainDamageType);

                _combat.SetInCombatMode(entity, prev);
            }
            else if (TryComp<MeleeWeaponComponent>(entity.Comp.Weapon, out var melee) && TryComp<ExecutionComponent>(entity.Comp.Weapon, out var execution))
            {
                var prev = _combat.IsInCombatMode(entity);
                _combat.SetInCombatMode(entity, true);

                execution.Executing = true;

                _popup.PopupEntity(Loc.GetString("execution-popup-melee-complete-external", ("attacker", Identity.Entity(entity, EntityManager)), ("victim", Identity.Entity(entity.Comp.Hostage, EntityManager))), entity, Shared.Popups.PopupType.LargeCaution);

                _melee.AttemptLightAttack(entity, entity.Comp.Weapon, melee, entity.Comp.Hostage);

                _audio.PlayPvs(melee.HitSound, entity.Comp.Weapon);

                _combat.SetInCombatMode(entity, prev);
                execution.Executing = false;
            }

        }

        StopHostaging(entity, entity.Comp.Hostage);

        return true;
    }



    private void StopHostaging(EntityUid hostageTaker, EntityUid hostage)
    {
        RemComp<HostageTakerComponent>(hostageTaker);
        RemComp<HostageComponent>(hostage);
        _modifier.RefreshMovementSpeedModifiers(hostageTaker);
    }

    private string GetDamage(DamageSpecifier damage, string? mainDamageType)
    {
        // Default fallback if nothing valid found
        mainDamageType ??= "Blunt";

        if (damage == null || damage.DamageDict.Count == 0)
            return mainDamageType;

        var filtered = damage.DamageDict
            .Where(kv => !string.Equals(kv.Key, "Structural", StringComparison.OrdinalIgnoreCase));

        if (filtered.Any())
        {
            mainDamageType = filtered.Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
        }

        return mainDamageType ?? "Blunt";
    }
}
