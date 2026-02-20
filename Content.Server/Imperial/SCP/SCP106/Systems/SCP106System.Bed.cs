using System.Linq;
using Content.Server.Imperial.SCP.SCP106.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Pulling.Components;
using System.Numerics;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using System.Threading.Tasks.Dataflow;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Imperial.SCP.SCP106.Systems;

public sealed partial class SCP106System : EntitySystem
{
    private void InitializeBed()
    {
        SubscribeLocalEvent<SCP106BedComponent, ComponentStartup>(OnBedInit);
        SubscribeLocalEvent<SCP106BedComponent, SignalReceivedEvent>(OnSignalReceived);
    }
    private void OnBedInit(EntityUid uid, SCP106BedComponent component, ComponentStartup args)
    {

        _signalSystem.EnsureSinkPorts(uid, component.TriggerPort);
    }

    private void OnSignalReceived(EntityUid uid, SCP106BedComponent component, ref SignalReceivedEvent args)
    {
        if (!TryComp<StrapComponent>(uid, out var strap))
            return;

        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);


        if (args.Port == component.TriggerPort)
        {
            if (state == SignalState.High || state == SignalState.Momentary)
            {
                ActivateBed(uid);
            }
        }
    }

    private void ActivateBed(EntityUid uid)
    {
        var transformTable = Transform(uid);
        if (!TryComp<SCP106BedComponent>(uid, out var comp))
            return;
        if (!TryComp<StrapComponent>(uid, out var strapComponent))
            return;
        if (comp.Started)
            return;
        var victims = strapComponent.BuckledEntities;
        if (victims.Count == 0)
            return;
        var victim = victims.FirstOrDefault();
        EnsureComp<BlockMovementComponent>(victim);
        RemComp<PullableComponent>(victim);
        //_buckleSystem.TryUnbuckle(victim, victim, popup: false);
        _audio.PlayGlobal(comp.ContainmentSound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
        var curTime = _gameTiming.CurTime;
        comp.Victim = victim;
        comp.DamageEnd = curTime + comp.DelayDamage;
        comp.ContainmentEnd = curTime + comp.DelayContainment;
        comp.Started = true;
        //_chat.DispatchFilteredAnnouncement(Filter.Broadcast(), )
    }

    private void UpdateBed()
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SCP106BedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CooldownEnd != TimeSpan.Zero)
            {
                if (comp.CooldownEnd < curTime)
                    continue;
                else
                    comp.CooldownEnd = TimeSpan.Zero;
            }
            if (comp.Started == false)
                continue;
            if (comp.DamageEnd <= curTime && comp.DamageEnd != TimeSpan.Zero)
            {
                if (!HasComp<StrapComponent>(uid))
                    continue;
                _damageable.TryChangeDamage(comp.Victim, comp.Damage);
                _chat.TryEmoteWithChat(comp.Victim, comp.PrototypeScream);
                comp.DamageEnd = TimeSpan.Zero;
            }
            if (comp.ContainmentEnd <= curTime && comp.ContainmentEnd != TimeSpan.Zero)
            {
                RemoveAllPuddles();
                comp.Started = false;
                comp.CooldownEnd = curTime + comp.Cooldown;
                var puddle = EntityUid.Invalid;
                var xquery = EntityQueryEnumerator<SCP106Component, TransformComponent, MetaDataComponent>();
                var transformTable = Transform(uid);
                if (transformTable == null)
                {
                    continue;
                }
                while (xquery.MoveNext(out var entity, out var scp, out var transform, out var metadata))
                {
                    var map = transformTable.MapID;
                    if (!_mapSystem.TryGetMap(map, out var mapEnt))
                    {
                        continue;
                    }
                    _transform.SetCoordinates((entity, transform, metadata), transformTable.Coordinates.Offset(new Vector2(0, -1)));
                    _transform.SetParent(entity, transform, mapEnt.Value);
                    var newb = TransformInto(entity, scp.GhostMorph);
                    var newscp = EnsureComp<SCP106Component>(newb);
                    newscp.InDimension = true;
                    newscp.InPocketDimension = false;
                    _actions.RemoveAction(newb, newscp.PuddleEnterDimensionEntity);
                    if (puddle == EntityUid.Invalid)
                    {
                        puddle = Spawn(scp.PuddleID, transformTable.Coordinates.Offset(new Vector2(0, -1)));
                        var compp = EnsureComp<SCP106PuddleComponent>(puddle);
                        compp.TargetMap = scp.PocketMapId;
                    }
                    scp.Puddles.Add(puddle);
                }
            }
        }
    }
}
