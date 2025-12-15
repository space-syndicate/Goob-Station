using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.Medieval.Magic.Mana;


public sealed partial class ManaSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManaComponent, ComponentStartup>(OnApplyManaModifiers);
        SubscribeLocalEvent<ManaComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<ManaDrainSpellComponent, MedievalBeforeCastSpellEvent>(OnBeforeCast);
        SubscribeLocalEvent<ManaDrainSpellComponent, MedievalAfterCastSpellEvent>(OnAfterCast);
    }

    public override void Update(float frameTime)
    {
        var enumerator = EntityQueryEnumerator<ManaComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (component.MaxMana == 0f) continue;
            if (_timing.CurTime <= component.EndTime) continue;

            component.EndTime = _timing.CurTime + component.ReloadTime;

            TryChargeMana(uid, component.Regen, component);

            if (_net.IsServer)
                _alertsSystem.ShowAlert(uid, component.ManaAlert, (short)Math.Clamp(Math.Round(component.Mana / component.MaxMana * 5.05f), 0, 5));
        }
    }

    public void OnApplyManaModifiers(EntityUid uid, ManaComponent component, ComponentStartup args)
    {
        component.MaxMana *= component.MaxManaRaceModifier;
        component.Regen *= component.RegenRaceModifier;

        if (TryComp<ManaTraitModifierComponent>(uid, out var trait))
        {
            component.MaxMana *= trait.MaxManaTraitModifier;
            component.Regen *= trait.RegenTraitModifier;
        }

        if (TryComp<ManaJobModifierComponent>(uid, out var job))
        {
            component.MaxMana *= job.MaxManaJobModifier;
            component.Regen *= job.RegenJobModifier;
        }

        component.Mana = component.MaxMana;
    }

    private void OnRejuvenate(EntityUid uid, ManaComponent component, RejuvenateEvent args)
    {
        _alertsSystem.ShowAlert(uid, component.ManaAlert, (short)Math.Clamp(Math.Round(component.Mana / component.MaxMana * 5.05f), 0, 5));

        TryChangeMana(uid, component.MaxMana);
    }

    private void OnBeforeCast(EntityUid uid, ManaDrainSpellComponent component, ref MedievalBeforeCastSpellEvent args)
    {
        if (args.Cancelled) return;

        if (!TryComp<ManaComponent>(args.Performer, out var manaComponent))
        {
            if (_timing.IsFirstTimePredicted && _net.IsServer) _popupSystem.PopupEntity(Loc.GetString("medieval-mana-cant-cast-spells"), args.Performer, args.Performer, PopupType.LargeCaution);
            args.Cancelled = true;

            return;
        }

        if (manaComponent.Mana - GetAllSpellsManaDrain(manaComponent.CastedSpells) - component.ManaDrain < 0)
        {
            if (component.CanUseWithoutMana) manaComponent.CastedSpells.TryAdd(uid, component.ManaDrain);
            if (_timing.IsFirstTimePredicted && _net.IsServer) _popupSystem.PopupEntity(Loc.GetString(component.ManaLowMessage), args.Performer, args.Performer, PopupType.LargeCaution);

            args.Cancelled = !component.CanUseWithoutMana;

            return;
        }

        manaComponent.CastedSpells.TryAdd(uid, component.ManaDrain);
    }

    private void OnAfterCast(EntityUid uid, ManaDrainSpellComponent component, ref MedievalAfterCastSpellEvent args)
    {
        if (!TryComp<ManaComponent>(args.Performer, out var manaComponent)) return;

        manaComponent.CastedSpells.Remove(uid);

        if (manaComponent.Mana - component.ManaDrain < 0)
            _damageableSystem.TryChangeDamage(args.Performer, component.DamageOnUseWithoutMana, true, false);

        TryChargeMana(args.Performer, -component.ManaDrain);

        if (_timing.IsFirstTimePredicted && _net.IsServer) _popupSystem.PopupEntity(Loc.GetString("medieval-mana-cast-spell", ("manaCost", component.ManaDrain)), args.Performer, args.Performer, PopupType.Large);
    }

    #region Helpers

    private float GetAllSpellsManaDrain(Dictionary<EntityUid, float> spells) => spells.Aggregate(0.0f, (sum, next) => sum + next.Value);

    #endregion

    #region Public API

    public bool TryChangeMana(EntityUid uid, float mana, ManaComponent? component = null)
    {
        if (!Resolve(uid, ref component)) return false;
        if (component.Mana > component.MaxMana) return false;

        component.Mana = mana < 0 ? 0 : mana;

        Dirty(uid, component);

        return true;
    }

    public bool TryChargeMana(EntityUid uid, float mana, ManaComponent? component = null)
    {
        if (!Resolve(uid, ref component)) return false;
        if (component.Mana + mana > component.MaxMana) return false;

        component.Mana = component.Mana + mana < 0 ? 0 : component.Mana + mana;

        Dirty(uid, component);

        return true;
    }

    #endregion
}
