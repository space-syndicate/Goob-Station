using System.Numerics;
using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class RegenerativeCrossbreedSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RegenerativeCrossbreedComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RegenerativeCrossbreedComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnAfterInteract(Entity<RegenerativeCrossbreedComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target
            || !HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;
        Apply(ent, target, args.User);
    }

    private void OnUseInHand(Entity<RegenerativeCrossbreedComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Apply(ent, args.User, args.User);
    }

    private void Apply(Entity<RegenerativeCrossbreedComponent> ent, EntityUid target, EntityUid user)
    {
        _effects.ApplyEffects(target, ent.Comp.Effects, user: user);
        _audio.PlayPvs(ent.Comp.Sound, target);

        foreach (var effect in ent.Comp.VisualEffects)
        {
            SpawnAttachedTo(effect, new EntityCoordinates(target, Vector2.Zero));
        }

        _popup.PopupEntity(Loc.GetString("slime-crossbreed-regenerative-used", ("target", target)), target, PopupType.Medium);
        _adminLog.Add(LogType.EntityEffect, LogImpact.Low, $"{ToPrettyString(user):user} used {ToPrettyString(ent):item} on {ToPrettyString(target):target}");

        QueueDel(ent);
    }
}
