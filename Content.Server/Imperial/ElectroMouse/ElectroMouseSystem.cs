using Content.Server.Actions;
using Content.Shared.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Imperial.ElectroMouse.Events;
using Content.Shared.Alert;
using Content.Shared.Revenant;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.ElectroMouse.Components;
using Content.Shared.Interaction;
using Content.Server.Power.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using System.Numerics;
using Robust.Server.GameObjects;
using Content.Shared.Revenant.Components;
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Content.Shared.Doors.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Timing;
using Content.Shared.Imperial.ElectroMouseShield.Components;
using Content.Server.Emp;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Server.Stunnable;
using Content.Shared.Ninja.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.SMES;
using Content.Server.Beam;
using Filter = Robust.Shared.Player.Filter;
using Robust.Shared.Physics;
using Content.Shared.Body.Components;
using CollisionGroup = Content.Shared.Physics.CollisionGroup;
using Content.Server.Chat.Systems;
using Content.Shared.Light.Components;

namespace Content.Server.Imperial.ElectroMouse.EntitySystems;

/*
　　　　　　　　　　_,.. -──- ､,
　　　　　　　　,　'" 　 　　　 　　 `ヽ.
　　　　　　 ／/¨7__　　/ 　 　 i　 _厂廴
　　　　　 /￣( ノ__/　/{　　　　} ｢　（_冫}
　　　　／￣l＿// 　/-|　 ,!　 ﾑ ￣|＿｢ ＼＿_
　　. イ　 　 ,　 /!_∠_　|　/　/_⊥_,ﾉ ハ　 　イ
　　　/ ／ / 　〃ん心 ﾚ'|／　ｆ,心 Y　i ＼_＿＞　
　 ∠イ 　/　 　ﾄ弋_ツ　　 　 弋_ﾂ i　 |　 | ＼
　 _／ _ノ|　,i　⊂⊃　　　'　　　⊂⊃ ./　 !､＿ン
　　￣　　∨|　,小、　　` ‐ ' 　　 /|／|　/
　 　 　 　 　 Y　|ﾍ＞ 、 ＿ ,.　イﾚ|　 ﾚ'
　　　　　　 r'.| 　|;;;入ﾞ亠―亠' );;;;;! 　|､
　　　　　 ,ノ:,:|.　!|く　__￣￣￣__У　ﾉ|:,:,ヽ
　　　　　(:.:.:.:ﾑ人!ﾍ　 　` ´ 　　 厂|ノ:.:.:丿
*/


public sealed partial class ElectroMouseSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    // [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectroMouseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseShopActionEvent>(OnShop);
        SubscribeLocalEvent<ElectroMouseComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<ElectroMouseComponent, HarvestEvent>(OnHarvest);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseOverloadLightsActionEvent>(OnOverloadLightsAction);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseDashEvent>(DashAbility);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseHealEvent>(OnHeal);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseShieldEvent>(OnShield);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseEmpEvent>(OnEmp);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseSpeedEvent>(OnSpeed);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseDoubleEvent>(OnDouble);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseUpgradeEvent>(OnUpgrade);
        SubscribeLocalEvent<ElectroMouseComponent, DrainDoAfterEvent>(OnDoAfterApc);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseLightningEvent>(OnLightning);
        SubscribeLocalEvent<ElectroMouseComponent, ElectroMouseElevationEvent>(OnElevation);
    }

    private void OnElevation(EntityUid uid, ElectroMouseComponent component, ElectroMouseElevationEvent args)
    {
        if (args.Handled || !TryComp<FixturesComponent>(uid, out var fixturesComponent) || !HasComp<BodyComponent>(uid))
            return;
        args.Handled = true;

        var fixture = fixturesComponent.Fixtures.First();
        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int)CollisionGroup.GhostImpassable, fixturesComponent);

        _audio.PlayGlobal(component.ElevationSound, Filter.Broadcast(), true, component.Params);

        _chat.DispatchGlobalAnnouncement(Loc.GetString("electromouse-elevation"), colorOverride: Color.Gold);

        AddEnergy(uid, component, 9999);

        component.DashEnergy += 2;
        component.EmpRadius += 7;
        component.Duration += 2;
        component.HealingStrength += 5;

        _action.RemoveAction(uid, component.Action);
    }

    public override void Update(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ElectroMouseComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (TryComp<MovementSpeedModifierComponent>(uid, out var movementSpeedModifier))
            {
                var speedmodifier = (float)component.Energy / 200 + 5f;
                if (component.IsSpeed && speedmodifier >= 15f)
                    speedmodifier = 15f;
                else if (!component.IsSpeed && speedmodifier >= 10f)
                    speedmodifier = 10f;
                _movementSpeedModifierSystem.ChangeBaseSpeed(uid, speedmodifier, speedmodifier, 20, movementSpeedModifier);
            }
            if (component.TimeUtilSpeed <= curTime && component.IsSpeed)
            {
                if (component.Energy > 1)
                {
                    AddEnergy(uid, component, -1);
                    component.TimeUtilSpeed = _gameTiming.CurTime + TimeSpan.FromSeconds(1.5);
                }
                else
                    component.IsSpeed = false;
            }
            if (movementSpeedModifier != null && component.IsSpeed)
            {
                var lookup = _lookup.GetEntitiesInRange(uid, 0.5f, LookupFlags.Approximate | LookupFlags.Static);
                foreach (var ent in lookup)
                {
                    if (HasComp<ApcPowerProviderComponent>(ent) && !component.IsChanged)
                    {
                        var newspeed = (float)movementSpeedModifier.BaseWalkSpeed * 2;
                        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, newspeed, newspeed, 20, movementSpeedModifier);

                        component.IsChanged = true;
                    }
                    else if (!HasComp<ApcPowerProviderComponent>(ent) && component.IsChanged)
                    {
                        var newspeed = (float)movementSpeedModifier.BaseWalkSpeed / 2;
                        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, newspeed, newspeed, 20, movementSpeedModifier);

                        component.IsChanged = false;
                        component.IsSpeed = false;
                    }
                }
            }
            if (TryComp<DamageableComponent>(uid, out var damageableComponent))
            {
                var total = FixedPoint2.Zero;
                foreach (var value in damageableComponent.Damage.DamageDict.Values)
                {
                    total += value;
                }
                if (total >= 30)
                {
                    Spawn(component.SpawnOnDeathPrototype, Transform(uid).Coordinates);
                    QueueDel(uid);
                }
            }
            if (component.TimeUtil <= curTime && component.IsActiveShield)
            {
                RemComp<ReflectComponent>(uid);
                component.IsActiveShield = false;
                RemComp<ElectroMouseShieldComponent>(uid);
            }
        }
    }

    private void OnUpgrade(EntityUid uid, ElectroMouseComponent component, ElectroMouseUpgradeEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        component.DashEnergy += 2;
        component.EmpRadius += 2;
        component.Duration += 2;
        component.HealingStrength += 5;
        if (!component.BuyedUpgrade)
        {
            component.Energy -= 500;
            component.BuyedUpgrade = true;
        }
    }

    private void OnDouble(EntityUid uid, ElectroMouseComponent component, ElectroMouseDoubleEvent args)
    {
        var query = EntityQueryEnumerator<ElectroMouseComponent>();
        var quanity = 0;

        while (query.MoveNext(out var _, out _))
            quanity++;

        if (quanity > 1)
        {
            _popup.PopupEntity("Слишком много шокомышей на станции", uid, uid);
            return;
        }
        if (!component.BuyedDouble)
        {
            component.Energy -= 300;
            component.BuyedDouble = true;
        }
        if (component.Energy <= 100 || args.Handled)
        {
            _popup.PopupEntity("Недостаточно энергии", uid, uid);
            return;
        }
        args.Handled = true;

        AddEnergy(uid, component, -100);
        Spawn("SpawnPointGhostElectroMouse", Transform(uid).Coordinates);
    }

    private void OnLightning(EntityUid uid, ElectroMouseComponent component, ElectroMouseLightningEvent args)
    {
        if (args.Handled || !_gameTiming.IsFirstTimePredicted)
            return;
        if (!component.BuyedLightning)
        {
            component.Energy -= 200;
            component.BuyedLightning = true;
        }
        if (component.Energy <= 10)
        {
            _popup.PopupEntity("Недостаточно энергии", uid, uid);
            return;
        }
        var target = args.Target;
        if (!TryComp<MobStateComponent>(target, out var stateComponent) && _mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель должна быть живым существом."), uid, uid);
            return;
        }
        if (!HasComp<TransformComponent>(target))
            return;
        args.Handled = true;

        AddEnergy(uid, component, -10);

        _stun.TryUpdateStunDuration(uid, TimeSpan.FromSeconds(2f));
        _beam.TryCreateBeam(uid, target, "LightningRevenant");
        _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(7f));
    }

    private void OnSpeed(EntityUid uid, ElectroMouseComponent component, ElectroMouseSpeedEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        args.Handled = true;
        if (!component.BuyedSpeed)
        {
            component.Energy -= 150;
            component.BuyedSpeed = true;
        }
        Dirty(uid, component);

        if (!component.CanSmesEtc)
            component.CanSmesEtc = true;
    }

    private void OnEmp(EntityUid uid, ElectroMouseComponent component, ElectroMouseEmpEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var coords = _transform.GetMapCoordinates(uid);
        if (!component.BuyedEMP)
        {
            component.Energy -= 75;
            component.BuyedEMP = true;
        }
        if (component.Energy <= 20)
        {
            _popup.PopupEntity("Недостаточно энергии", uid, uid);
            return;
        }
        _empSystem.EmpPulse(coords, component.EmpRadius, 10000, 120);

        AddEnergy(uid, component, -20);

        if (TryComp<PointLightComponent>(uid, out var pointLightComponent))
        {
            var newenerg = pointLightComponent.Energy + 2.5f;
            var newrad = pointLightComponent.Radius + 0.2f;

            _pointLight.SetEnergy(uid, newenerg, pointLightComponent);
            _pointLight.SetRadius(uid, newrad, pointLightComponent);
        }
    }

    private void OnShield(EntityUid uid, ElectroMouseComponent component, ElectroMouseShieldEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        if (!component.CanAPC)
            component.CanAPC = true;
        if (!component.BuyedShield)
        {
            component.Energy -= 125;
            component.BuyedShield = true;
        }
        if (component.Energy <= 20)
        {
            _popup.PopupEntity("Недостаточно энергии", uid, uid);
            return;
        }
        if (!HasComp<ReflectComponent>(uid))
        {
            AddComp<ReflectComponent>(uid);
            if (!TryComp<ReflectComponent>(uid, out var reflectComponent) || !TryComp<PointLightComponent>(uid, out var pointLightComponent))
                return;
            component.IsActiveShield = true;
            AddEnergy(uid, component, -20);
            AddComp<ElectroMouseShieldComponent>(uid);
            reflectComponent.ReflectProb = 1.0f;
            var newenerg = pointLightComponent.Energy + 2.5f;
            var newrad = pointLightComponent.Radius + 0.2f;
            _pointLight.SetEnergy(uid, newenerg, pointLightComponent);
            _pointLight.SetRadius(uid, newrad, pointLightComponent);
            Dirty(uid, pointLightComponent);
            component.TimeUtil = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Duration);
            if (!component.CanAPC)
                component.CanAPC = true;
        }
    }

    private void OnShop(EntityUid uid, ElectroMouseComponent component, ElectroMouseShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    private void OnMapInit(EntityUid uid, ElectroMouseComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, component.ShopId);
    }

    private void OnInteract(EntityUid uid, ElectroMouseComponent component, UserActivateInWorldEvent args)
    {
        var target = args.Target;

        if (HasComp<PoweredLightComponent>(target))
        {
            args.Handled = _ghost.DoGhostBooEvent(target);
            return;
        }

        if (component.Harvested.Contains(target))
        {
            _popup.PopupEntity(Loc.GetString("electromouse-harvested"), uid, uid);
            return;
        }

        if (HasComp<DoorComponent>(target))
            return;

        if (HasComp<ApcPowerReceiverComponent>(target) || HasComp<HitscanBatteryAmmoProviderComponent>(target) || HasComp<ProjectileBatteryAmmoProviderComponent>(target) || HasComp<PowerNetworkBatteryComponent>(target))
        {
            args.Handled = true;

            BeginHarvestDoAfter(uid, target, component);
        }
    }

    private void DashAbility(EntityUid uid, ElectroMouseComponent comp, ElectroMouseDashEvent args)
    {
        if (!HasComp<ElectroMouseComponent>(uid))
            return;

        if (args.Handled)
            return;
        if (!comp.BuyedDash)
        {
            comp.Energy -= 25;
            comp.BuyedDash = true;
        }
        args.Handled = true;

        var entityManager = IoCManager.Resolve<IEntityManager>();

        var xformSystem = entityManager.System<TransformSystem>();

        var energy = comp.DashEnergy;

        var mapPosition = xformSystem.GetWorldPosition(uid);
        var reactionBounds = new Box2(mapPosition - new Vector2(energy, energy), mapPosition + new Vector2(energy, energy));

        var newPosition = comp.Coordinates;

        newPosition = GetPositionFromRotation(reactionBounds, energy, uid);

        if (newPosition != null)
            xformSystem.SetWorldPosition(
                uid,
                (Vector2)newPosition
            );
        _audio.PlayPvs(comp.DashSound, uid);
    }

    private static Vector2 GetPositionFromRotation(Box2 reactionBounds, float energy, EntityUid uid)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();

        var xformSystem = entityManager.System<TransformSystem>();

        var resultVector = Angle.FromDegrees(45).RotateVec(
            xformSystem.GetWorldRotation(uid).RotateVec(new Vector2(energy, energy))
        );

        return reactionBounds.Center - resultVector;
    }

    private bool ChangeEnergyAmount(EntityUid uid, FixedPoint2 amount, ElectroMouseComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Energy + amount <= 0)
            return false;
        if (!HasComp<DamageableComponent>(uid))
            return false;
        component.Energy += amount;

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, component.EssenceAlert, (short)Math.Clamp(Math.Round(component.Energy.Float() / 10f), 0, 16));

        return true;
    }

    private void AddEnergy(EntityUid uid, ElectroMouseComponent component, FixedPoint2 energ)
    {
        var store = EnsureComp<StoreComponent>(uid);
        // Get the ElectroMouseComponent instance from the entity
        if (TryComp<ElectroMouseComponent>(uid, out var electroMouseComponent))
        {
            ChangeEnergyAmount(uid, energ, component);
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { component.StolenEnergyCurrencyPrototype, energ } }, uid, store);
        }
    }

    // private void Inject(EntityUid target, string reagent, FixedPoint2 value)
    // {
    //     if (value <= 0 || reagent == null || !HasComp<BodyComponent>(target) || !_solutionContainer.TryGetInjectableSolution(target, out var injsol, out _))
    //         return;
    //     _solutionContainer.TryAddReagent(injsol.Value, reagent, value);
    // }

    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, ElectroMouseComponent comp)
    {
        if (TryComp<HitscanBatteryAmmoProviderComponent>(target, out var hitprov) && hitprov.Shots == 0)
            return;
        if (TryComp<ProjectileBatteryAmmoProviderComponent>(target, out var projprov) && projprov.Shots == 0)
            return;
        if (HasComp<HitscanBatteryAmmoProviderComponent>(target) && !comp.CanBattery)
            return;
        if (HasComp<ProjectileBatteryAmmoProviderComponent>(target) && !comp.CanBattery)
            return;

        if (HasComp<PowerNetworkBatteryComponent>(target))
        {
            DoAfterApcEtc(uid, comp, target);
            return;
        }
        if (HasComp<LitOnPoweredComponent>(target) && !_pointLight.IsPowered(target, EntityManager))
            return;
        var doAfter = new DoAfterArgs(EntityManager, uid, comp.HarvestDebuffs.X, new HarvestEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;
        _stun.TryUpdateStunDuration(uid, TimeSpan.FromSeconds(5f));

        _popup.PopupEntity(Loc.GetString("electromouse-startharvest", ("target", target)),
            target, PopupType.Large);
    }

    private void DoAfterApcEtc(EntityUid uid, ElectroMouseComponent component, EntityUid target)
    {
        var doAfterApc = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(1f), new DrainDoAfterEvent(), target: target, eventTarget: uid)
        {
            MovementThreshold = 0.5f,
            BreakOnMove = true,
            CancelDuplicate = false,
        };

        if (HasComp<PowerNetworkBatteryComponent>(target) && component.CanSmesEtc)
        {
            _doAfter.TryStartDoAfter(doAfterApc);
            return;
        }
        if (HasComp<ApcComponent>(target) && component.CanAPC)
            _doAfter.TryStartDoAfter(doAfterApc);
    }

    private void OnDoAfterApc(EntityUid uid, ElectroMouseComponent component, DrainDoAfterEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        args.Repeat = TryDrainPower(uid, component, args.Target.Value);
    }

    private bool TryDrainPower(EntityUid uid, ElectroMouseComponent component, EntityUid target)
    {
        if (!TryComp<BatteryComponent>(target, out var targetBattery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return false;

        if (targetBattery.CurrentCharge <= targetBattery.MaxCharge / 100 * 20)
        {
            _popup.PopupEntity(Loc.GetString("battery-drainer-empty", ("battery", target)), uid, uid, PopupType.Medium);
            return false;
        }

        var available = targetBattery.CurrentCharge;
        int required;

        if (HasComp<ApcComponent>(target))
        {
            required = 100;
        }
        else if (HasComp<SmesComponent>(target))
            required = 8500;
        else
            required = 5000;

        _battery.UseCharge(target, required * 200, targetBattery);

        Dirty(target, targetBattery);

        var maxDrained = pnb.MaxSupply;
        var input = Math.Min(Math.Min(available, required / 0.001f), maxDrained);
        if (HasComp<ApcComponent>(target))
            input *= 10;
        if (HasComp<SmesComponent>(target))
            input *= 2;
        var output = input * 0.0001f;

        AddEnergy(uid, component, output);

        Spawn("EffectSparks", Transform(target).Coordinates);

        _audio.PlayPvs(component.SparkSound, target);

        // repeat the doafter until we get lower than 20%
        return true;
    }

    private void OnHarvest(EntityUid uid, ElectroMouseComponent component, HarvestEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        _popup.PopupEntity(Loc.GetString("electromouse-endharvest", ("target", target)),
            target, PopupType.Large);

        if (HasComp<ApcPowerReceiverComponent>(target))
        {
            component.Harvested.Add(target);
            AddEnergy(uid, component, 5);
        }
        else if (TryComp<HitscanBatteryAmmoProviderComponent>(target, out var hitprov))
        {
            AddEnergy(uid, component, hitprov.Shots);
            hitprov.Shots = 0;
            Dirty(target, hitprov);
        }
        else if (TryComp<ProjectileBatteryAmmoProviderComponent>(target, out var projprov))
        {
            AddEnergy(uid, component, projprov.Shots);
            projprov.Shots = 0;
            Dirty(target, projprov);
        }
        args.Handled = true;
    }

    private void OnOverloadLightsAction(EntityUid uid, ElectroMouseComponent component, ElectroMouseOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;


        if (!component.CanBattery)
            component.CanBattery = true; //at first overload make can eat from laser guns

        args.Handled = true;
        if (!component.BuyedOverload)
        {
            component.Energy -= 50;
            component.BuyedOverload = true;
        }
        if (component.Energy <= 25)
        {
            _popup.PopupEntity("Недостаточно энергии", uid, uid);
            return;
        }
        AddEnergy(uid, component, -25);

        var xform = Transform(uid);
        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var mobState = GetEntityQuery<MobStateComponent>();
        var lookup = _lookup.GetEntitiesInRange(uid, component.OverloadRadius);
        //TODO: feels like this might be a sin and a half
        foreach (var ent in lookup)
        {
            if (!HasComp<MobStateComponent>(ent)) continue; // Imperial Space overload-lights-fix
            if (!_mobState.IsAlive(ent)) continue; // Imperial Space overload-lights-fix

            var nearbyLights = _lookup.GetEntitiesInRange(ent, component.OverloadZapRadius) // Imperial Space overload-lights-fix
                .Where(e => !HasComp<RevenantOverloadedLightsComponent>(e) &&
                            _interact.InRangeUnobstructed(e, uid, -1)).ToArray();

            if (!nearbyLights.Any())
                continue;

            //get the closest light
            var allLight = nearbyLights.OrderBy(e =>
                Transform(e).Coordinates.TryDistance(EntityManager, xform.Coordinates, out var dist) ? component.OverloadZapRadius : dist);
            var comp = EnsureComp<RevenantOverloadedLightsComponent>(allLight.First());
            comp.Target = ent; //who they gon fire at?
        }
    }

    private void OnHeal(EntityUid uid, ElectroMouseComponent component, ElectroMouseHealEvent args)
    {
        if (args.Handled)
            return;
        if (!component.BuyedHeal)
        {
            component.Energy -= 100;
            component.BuyedHeal = true;
        }
        if (component.Energy <= 30)
        {
            _popup.PopupEntity("Недостаточно энергии", uid, uid);
            return;
        }
        if (TryComp<DamageableComponent>(uid, out var damagecomp) && component.Energy >= 30)
        {
            args.Handled = true;
            var result = damagecomp.Damage.DamageDict.OrderByDescending(z => z.Value).ToDictionary(a => a, s => s).First().Key.Key.ToString();
            if (component.HealingStrength == 10)
            {
                if (result != null && damagecomp.Damage.DamageDict[result] != 0)
                {
                    AddEnergy(uid, component, -30);
                    var newdamage = component.HealingStrength;
                    if (damagecomp.Damage.DamageDict[result] <= newdamage)
                        newdamage = (int)damagecomp.Damage.DamageDict[result];
                    DamageSpecifier damage = new()
                    {
                        DamageDict = new()
                        {
                            { result, component.HealingStrength * -1 }
                        }
                    };
                    _damageable.TryChangeDamage(uid, damage, false, true, damagecomp, origin: uid);
                    Dirty(uid, damagecomp);
                }
                else
                    _popup.PopupEntity("Вы не ранены", uid, uid);
            }
            else
            {
                if (result != null && damagecomp.Damage.DamageDict[result] != 0)
                {
                    AddEnergy(uid, component, -30);
                    var newdamage = component.HealingStrength;
                    if (damagecomp.Damage.DamageDict[result] <= newdamage)
                        newdamage = (int)damagecomp.Damage.DamageDict[result];
                    DamageSpecifier damage = new()
                    {
                        DamageDict = new()
                        {
                            { result, component.HealingStrength * -1 }
                        }
                    };
                    _damageable.TryChangeDamage(uid, damage, false, true, damagecomp, origin: uid);
                    Dirty(uid, damagecomp);
                }
                else
                    _popup.PopupEntity("Вы не ранены", uid, uid);
            }
        }
    }
}

