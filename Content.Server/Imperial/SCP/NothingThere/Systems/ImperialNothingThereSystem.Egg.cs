using Content.Shared.Imperial.SCP.NothingThere.Events;
using Content.Shared.Popups;
using Content.Shared.Mobs.Components;
using Content.Server.Imperial.SCP.NothingThere.Components;

namespace Content.Server.Imperial.SCP.NothingThere.Systems;

public sealed partial class ImperialNothingThereSystem
{

    private void InitializeEgg()
    {
        SubscribeLocalEvent<ImperialNothingThereComponent, ImperialNothingThereEggEvent>(OnEggAction);
    }

    private void OnEggAction(Entity<ImperialNothingThereComponent> ent, ref ImperialNothingThereEggEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<MobStateComponent>(args.Performer, out var mob))
            return;
        if (!TryComp<ImperialNothingThereComponent>(args.Performer, out var comp))
            return;
        if (!_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            return;
        args.Handled = true;
        if (comp.KillCount < comp.KillsRequired)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("nothingthere-hammaggotson-morekills"),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }
        else
        {
            _polymorph.PolymorphEntity(args.Performer, comp.EggMorph);
        }
    }

    private void UpdateEgg()
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ImperialNothingThereComponent>();
        while (query.MoveNext(out var entity, out var egg))
        {
            if (curTime >= egg.EggTransformEnd && egg.Phase == NothingTherePhase.Egg)
            {
                if (!_mind.TryGetMind(entity, out var mindId, out var mind))
                    continue;
                var newb = _polymorph.PolymorphEntity(entity, egg.TrueMorph) ?? EntityUid.Invalid;
                _audio.PlayPvs(egg.HatchSound, newb);
            }
        }
    }
}
