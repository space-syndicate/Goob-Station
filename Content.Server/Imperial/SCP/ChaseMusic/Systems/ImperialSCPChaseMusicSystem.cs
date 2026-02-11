using Content.Server.Imperial.SCP.ChaseMusic.Components;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Imperial.SCP.ChaseMusic.Events;

namespace Content.Server.Imperial.SCP.ChaseMusic.Systems;

public sealed class ImperialSCPChaseMusicSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ImperialSCPChaseMusicComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ImperialSCPChaseMusicComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ImperialSCPChaseMusicComponent, ImperialSCPChaseMusicActionEvent>(OnToggleChaseMusic);
        SubscribeLocalEvent<ImperialSCPChaseMusicComponent, MobStateChangedEvent>(OnMobStateChanged);
    }
    
    private void OnInit(EntityUid uid, ImperialSCPChaseMusicComponent comp, ComponentInit args)
    {
        _actions.AddAction(uid, ref comp.ChaseMusicToggleActionEntity, comp.ChaseMusicToggleAction);
    }
    
    private void OnRemove(EntityUid uid, ImperialSCPChaseMusicComponent comp, ComponentRemove args)
    {
        StopChaseMusic(uid, comp);
        
        if (comp.ChaseMusicToggleActionEntity != null)
            _actions.RemoveAction(uid, comp.ChaseMusicToggleActionEntity.Value);
    }
    
    private void OnToggleChaseMusic(EntityUid uid, ImperialSCPChaseMusicComponent comp, ImperialSCPChaseMusicActionEvent args)
    {
        if (args.Handled)
            return;
        
        if (comp.IsPlaying)
        {
            StopChaseMusic(uid, comp);
        }
        else
        {
            StartChaseMusic(uid, comp);
        }
        
        args.Handled = true;
    }
    
    public void StartChaseMusic(EntityUid uid, ImperialSCPChaseMusicComponent comp)
    {
        if (comp.IsPlaying)
            return;
        comp.PlayingStream = _audio.PlayPvs(
            comp.ChaseSound,
            uid,
            AudioParams.Default.WithLoop(true)
        )?.Entity;
        
        comp.IsPlaying = true;
    }
    
    public void StopChaseMusic(EntityUid uid, ImperialSCPChaseMusicComponent comp)
    {
        if (!comp.IsPlaying)
            return;
        if (comp.PlayingStream != null)
        {
            _audio.Stop(comp.PlayingStream.Value);
            comp.PlayingStream = null;
        }
        comp.IsPlaying = false;
    }
    private void OnMobStateChanged(EntityUid uid, ImperialSCPChaseMusicComponent comp, MobStateChangedEvent args)
    {
        if (_mobStateSystem.IsDead(uid) || _mobStateSystem.IsCritical(uid))
        {
            StopChaseMusic(uid, comp);
            if (comp.ChaseMusicToggleActionEntity != null)
            {
                _actions.RemoveAction(uid, comp.ChaseMusicToggleActionEntity);
                comp.ChaseMusicToggleActionEntity = null;
            }
        }
        else if (_mobStateSystem.IsAlive(uid))
        {
            if (comp.ChaseMusicToggleActionEntity == null && !string.IsNullOrEmpty(comp.ChaseMusicToggleAction))
            {
                _actions.AddAction(uid, ref comp.ChaseMusicToggleActionEntity, comp.ChaseMusicToggleAction);
            }
        }
    }
}