using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing; // Imperial Space "plasma Cutter + Advanced Version" Start
using Robust.Shared.Audio.Systems; // Imperial Space "plasma Cutter + Advanced Version" Start

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; // Imperial Space "plasma Cutter + Advanced Version" Start
    [Dependency] private readonly SharedAudioSystem _audio = default!; // Imperial Space "plasma Cutter + Advanced Version" Start
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, BatteryWeaponFireModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        var fireMode = GetMode(component);

        if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", proto.Name)));
    }

    private BatteryWeaponFireMode GetMode(BatteryWeaponFireModesComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }

    private void OnGetVerb(EntityUid uid, BatteryWeaponFireModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.FireModes.Count < 2)
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, uid))
            return;

        for (var i = 0; i < component.FireModes.Count; i++)
        {
            var fireMode = component.FireModes[i];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = i == component.CurrentFireMode,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetFireMode(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnUseInHandEvent(EntityUid uid, BatteryWeaponFireModesComponent component, UseInHandEvent args)
    {
        if(args.Handled)
            return;

        args.Handled = true;
        TryCycleFireMode(uid, component, args.User);
    }

    public void TryCycleFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, EntityUid? user = null)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        TrySetFireMode(uid, component, index, user);
    }

    public bool TrySetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.FireModes.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, uid))
            return false;

        // Imperial Space "plasma Cutter + Advanced Version" Start
        if (_gameTiming.CurTime < component.NextModeSwitchTime)
        {
            if (user != null)
            {
                var timeLeft = component.NextModeSwitchTime - _gameTiming.CurTime;
                _popupSystem.PopupClient(
                    Loc.GetString("gun-mode-switch-delay"),
                    uid,
                    user.Value);
            }
            return false;
        }

        component.NextModeSwitchTime = _gameTiming.CurTime + component.ModeSwitchDelay;
        Dirty(uid, component);
        // Imperial Space "plasma Cutter + Advanced Version" End

        SetFireMode(uid, component, index, user);

        return true;
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearanceSystem.SetData(uid, BatteryWeaponFireModeVisuals.State, prototype.ID, appearance);

            if (user != null)
            { // Imperial Space "plasma Cutter + Advanced Version"
                _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, user.Value);
                TryPlayModeSwitchSound(uid, component, user); // Imperial Space "plasma Cutter + Advanced Version"
            } // Imperial Space "plasma Cutter + Advanced Version"
        }

        component.NextModeSwitchTime = _gameTiming.CurTime + component.ModeSwitchDelay;  // Imperial Space "plasma Cutter + Advanced Version"
        Dirty(uid, component);  // Imperial Space "plasma Cutter + Advanced Version"

        if (TryComp(uid, out BatteryAmmoProviderComponent? batteryAmmoProviderComponent))
        {
            batteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            batteryAmmoProviderComponent.FireCost = fireMode.FireCost;

            Dirty(uid, batteryAmmoProviderComponent);

            _gun.UpdateShots((uid, batteryAmmoProviderComponent));
        }
    }


    // Imperial Space "plasma Cutter + Advanced Version" Start
    private bool TryPlayModeSwitchSound(EntityUid uid, BatteryWeaponFireModesComponent comp, EntityUid? user)
    {
        if (user == null || !Exists(uid))
            return false;

        _audio.PlayPredicted(comp.ModeSwitchSound, uid, user);
        return true;
    }
    // Imperial Space "plasma Cutter + Advanced Version" End
}
