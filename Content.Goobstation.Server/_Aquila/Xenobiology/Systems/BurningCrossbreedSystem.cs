using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class BurningCrossbreedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurningCrossbreedComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<BurningCrossbreedComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || HasComp<ActiveBurningCrossbreedComponent>(ent))
            return;

        args.Handled = true;

        var active = AddComp<ActiveBurningCrossbreedComponent>(ent);
        active.ActivateAt = _timing.CurTime + ent.Comp.Delay;
        active.User = args.User;

        _jitter.AddJitter(ent, ent.Comp.JitterAmplitude, ent.Comp.JitterFrequency);
        _audio.PlayPvs(ent.Comp.ActivateSound, ent);

        if (ent.Comp.ActivateEffect is { } effect)
            SpawnAtPosition(effect, Transform(ent).Coordinates);

        _popup.PopupEntity(Loc.GetString("slime-crossbreed-burning-activated", ("item", ent)), ent, PopupType.MediumCaution);
        _adminLog.Add(LogType.EntityEffect, LogImpact.Medium, $"{ToPrettyString(args.User):user} activated {ToPrettyString(ent):item}");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveBurningCrossbreedComponent, BurningCrossbreedComponent>();
        while (query.MoveNext(out var uid, out var active, out var burning))
        {
            if (_timing.CurTime < active.ActivateAt)
                continue;

            if (burning.Effect is { } effect)
                SpawnAtPosition(effect, Transform(uid).Coordinates);

            _effects.ApplyEffects(uid, burning.Effects, user: active.User);
            QueueDel(uid);
        }
    }
}
