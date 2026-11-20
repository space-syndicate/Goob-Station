using System;
using Content.Shared._BlackM.WallMedkit;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.WallMedkit;

public sealed class WallMedkitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WallMedkitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WallMedkitComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<WallMedkitComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WallMedkitComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WallMedkitComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AnimationEnd is not { } end || now < end)
                continue;

            comp.AnimationEnd = null;
            _appearance.SetData(uid, WallMedkitVisuals.Using, false);
        }
    }

    private void OnMapInit(Entity<WallMedkitComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Charges = Math.Clamp(ent.Comp.Charges, 0, ent.Comp.MaxCharges);
        UpdateAppearance(ent);
    }

    private void OnInteractHand(Entity<WallMedkitComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryInject(ent, args.User);
    }

    public bool TryInject(Entity<WallMedkitComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;
        var now = _timing.CurTime;

        if (now < comp.NextUse)
            return false;

        if (comp.Charges <= 0)
        {
            comp.NextUse = now + comp.UseDelay;
            _popup.PopupEntity(Loc.GetString("wall-medkit-empty"), ent, user, PopupType.SmallCaution);
            _audio.PlayPvs(comp.EmptySound, ent);
            return false;
        }

        if (!TryComp<BloodstreamComponent>(user, out var bloodstream))
        {
            _popup.PopupEntity(Loc.GetString("wall-medkit-no-bloodstream"), ent, user);
            return false;
        }

        var solution = new Solution(comp.Reagent, comp.Quantity);
        if (!_bloodstream.TryAddToChemicals((user, bloodstream), solution))
        {
            _popup.PopupEntity(Loc.GetString("wall-medkit-bloodstream-full"), ent, user, PopupType.SmallCaution);
            return false;
        }

        if (comp.Heal != null)
            _damageable.TryChangeDamage(user, comp.Heal, ignoreResistances: true, interruptsDoAfters: false, origin: ent);

        comp.Charges--;
        comp.NextUse = now + comp.UseDelay;
        comp.AnimationEnd = now + comp.UseDelay;

        _appearance.SetData(ent, WallMedkitVisuals.Using, true);
        UpdateAppearance(ent);
        _audio.PlayPvs(comp.InjectSound, ent);

        _popup.PopupEntity(Loc.GetString("wall-medkit-injected-self", ("charges", comp.Charges), ("max", comp.MaxCharges)),
            ent, user);
        _popup.PopupEntity(Loc.GetString("wall-medkit-injected-others", ("user", Identity.Entity(user, EntityManager))),
            ent, Filter.PvsExcept(user), true);

        return true;
    }

    private void OnInteractUsing(Entity<WallMedkitComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<WallMedkitRefillComponent>(args.Used, out var refill))
            return;

        args.Handled = true;
        var comp = ent.Comp;

        if (comp.Charges >= comp.MaxCharges)
        {
            _popup.PopupEntity(Loc.GetString("wall-medkit-refill-full"), ent, args.User);
            return;
        }

        comp.Charges = Math.Min(comp.MaxCharges, comp.Charges + Math.Max(1, refill.Charges));
        QueueDel(args.Used);

        UpdateAppearance(ent);
        _audio.PlayPvs(comp.RefillSound, ent);
        _popup.PopupEntity(Loc.GetString("wall-medkit-refilled", ("charges", comp.Charges), ("max", comp.MaxCharges)),
            ent, args.User);
    }

    private void OnExamined(Entity<WallMedkitComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("wall-medkit-examine",
            ("charges", ent.Comp.Charges), ("max", ent.Comp.MaxCharges)));
    }

    private void UpdateAppearance(Entity<WallMedkitComponent> ent)
    {
        _appearance.SetData(ent, WallMedkitVisuals.Charged, ent.Comp.Charges > 0);
    }
}
