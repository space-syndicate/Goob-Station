using Content.Server.Imperial.XxRaay.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Imperial.XxRaay.Nda079.Events;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Nda079;

public sealed class NDA079AirlockAbilitySystem : SharedNDA079AirlockAbilitySystem
{
    [Dependency] private readonly AlertEnergySystem _energySystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NDA079CpuSystem _cpuSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NDA079AirlockActionEvent>(OnAirlockAction);
    }

    protected override void OnAirlockVerbAct(EntityUid user, EntityUid target)
    {
    }

    private void OnAirlockAction(NDA079AirlockActionEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryGetEntity(ev.Target, out var target))
            return;

        if (!TryComp<NDA079AirlockAbilityComponent>(user, out var abilityComp))
            return;

        if (!TryComp<AlertEnergyComponent>(user, out var energyComp))
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-failed"), user.Value, user.Value);
            return;
        }

        if (energyComp.Energy < abilityComp.EnergyCost)
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-energyfailed",
                    ("EnergyCost", abilityComp.EnergyCost),
                    ("Energy", energyComp.Energy.ToString("F1"))),
                user.Value,
                user.Value);
            return;
        }

        var curTime = _gameTiming.CurTime;
        if (abilityComp.LastUsedTime.HasValue)
        {
            var timeSinceLastUse = curTime - abilityComp.LastUsedTime.Value;
            var cooldown = TimeSpan.FromSeconds(abilityComp.CooldownSeconds);
            if (timeSinceLastUse < cooldown)
            {
                var remaining = cooldown - timeSinceLastUse;
                _popup.PopupEntity(Loc.GetString("nda079-ability-cooldown",
                    ("remaining", remaining.TotalSeconds.ToString("F1"))),
                    user.Value,
                    user.Value);
                return;
            }
        }

        if (!_random.Prob(abilityComp.SuccessChance))
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-failed"), user.Value, user.Value);
            _energySystem.ModifyEnergy(user.Value, -abilityComp.EnergyCost, energyComp);
            abilityComp.LastUsedTime = curTime;
            Dirty(user.Value, abilityComp);
            _cpuSystem.AddCpuPoint(user.Value);

            if (TryComp<NDA079Component>(user.Value, out var nda079CompFail))
            {
                nda079CompFail.AirlockAbilityLastUsedTime = curTime;
                Dirty(user.Value, nda079CompFail);
            }
            return;
        }


        _energySystem.ModifyEnergy(user.Value, -abilityComp.EnergyCost, energyComp);
        abilityComp.LastUsedTime = curTime;
        Dirty(user.Value, abilityComp);
        _cpuSystem.AddCpuPoint(user.Value);

        if (TryComp<NDA079Component>(user.Value, out var nda079Comp))
        {
            nda079Comp.AirlockAbilityLastUsedTime = curTime;
            Dirty(user.Value, nda079Comp);
        }

        var success = false;
        switch (ev.ActionType)
        {
            case NDA079AirlockActionType.Toggle:
                success = HandleToggleDoor(target.Value, user.Value);
                break;
            case NDA079AirlockActionType.Bolt:
                success = HandleBoltDoor(target.Value, user.Value, abilityComp);
                break;
        }

        if (!success)
        {
            _popup.PopupEntity(Loc.GetString("nda079-ability-failed"), user.Value, user.Value);
        }
    }

    private bool HandleToggleDoor(EntityUid door, EntityUid user)
    {
        if (!TryComp<DoorComponent>(door, out var doorComp))
            return false;

        var isOpen = doorComp.State == DoorState.Open || doorComp.State == DoorState.Opening;

        if (isOpen)
        {
            _doorSystem.StartClosing(door, doorComp, user, predicted: true);
            return true;
        }

        if (doorComp.State == DoorState.Closed || doorComp.State == DoorState.Closing || doorComp.State == DoorState.Denying)
        {
            _doorSystem.StartOpening(door, doorComp, user, predicted: true);
            return true;
        }

        return false;
    }

    private bool HandleBoltDoor(EntityUid door, EntityUid user, NDA079AirlockAbilityComponent abilityComp)
    {
        if (!TryComp<DoorBoltComponent>(door, out var boltComp))
            return false;

        if (!_doorSystem.TrySetBoltDown((door, boltComp), true, user, predicted: true))
            return false;

        var duration = TimeSpan.FromSeconds(abilityComp.BoltDurationSeconds);
        Timer.Spawn(duration, () =>
        {
            if (!Exists(door) || !TryComp<DoorBoltComponent>(door, out var bolt))
                return;

            _doorSystem.TrySetBoltDown((door, bolt), false, null, predicted: false);
        });

        return true;
    }
}
