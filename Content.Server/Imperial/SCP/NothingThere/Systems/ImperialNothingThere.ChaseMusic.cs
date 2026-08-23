using Content.Server.Imperial.SCP.NothingThere.Components;
using Robust.Shared.Audio;
using Content.Shared.Mobs;

namespace Content.Server.Imperial.SCP.NothingThere.Systems;

public sealed partial class ImperialNothingThereSystem
{

    private void InitializeChaseMusic()
    {
        SubscribeLocalEvent<ImperialNothingThereComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ImperialNothingThereComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ImperialNothingThereComponent, MobStateChangedEvent>(OnMobStateChanged);
    }
    private void OnRemove(EntityUid uid, ImperialNothingThereComponent comp, ComponentRemove args)
    {
        StopChaseMusic(uid, comp);
    }

    public void StartChaseMusic(EntityUid uid, ImperialNothingThereComponent comp)
    {
        if (comp.IsPlaying)
            return;
        if (comp.Phase == NothingTherePhase.True)
            return;
        comp.PlayingStream = _audio.PlayPvs(
            comp.ChaseSound,
            uid,
            AudioParams.Default.WithLoop(true)
        )?.Entity;

        comp.IsPlaying = true;
    }

    public void StopChaseMusic(EntityUid uid, ImperialNothingThereComponent comp)
    {
        if (!comp.IsPlaying)
            return;
        if (comp.Phase == NothingTherePhase.True)
            return;
        if (comp.PlayingStream != null)
        {
            _audio.Stop(comp.PlayingStream.Value);
            comp.PlayingStream = null;
        }
        comp.IsPlaying = false;
    }
    private void OnMobStateChanged(EntityUid uid, ImperialNothingThereComponent comp, MobStateChangedEvent args)
    {
        if (_mobStateSystem.IsDead(uid) || _mobStateSystem.IsCritical(uid))
        {
            StopChaseMusic(uid, comp);
        }
        else if (_mobStateSystem.IsAlive(uid))
        {
            StartChaseMusic(uid, comp);
        }
    }
}
