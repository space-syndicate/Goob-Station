using Robust.Shared.Timing;
using Content.Shared.Examine;
using Content.Shared.Imperial.Medieval.Magic.Mana;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Imperial.Medieval.Magic.ManaRegen;


public sealed partial class ManaRegenSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ManaSystem _manaSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManaRegenComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ManaRegenComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ManaRegenComponent, GotUnequippedEvent>(OnUnequip);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ManaRegenComponent>();

        while (enumerator.MoveNext(out var _, out var component))
        {
            if (component.Equipee == null) continue;
            if (_timing.CurTime <= component.EndTime) continue;

            component.EndTime = _timing.CurTime + component.ReloadTime;

            if (!HasComp<ManaComponent>(component.Equipee.Value)) continue;

            _manaSystem.TryChargeMana(component.Equipee.Value, component.Regen);
        }
    }

    private void OnExamine(EntityUid uid, ManaRegenComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(component.ManaRegenMessage, ("regen", Math.Round(component.Regen, 3))));
    }

    private void OnEquip(EntityUid uid, ManaRegenComponent component, GotEquippedEvent args)
    {
        component.Equipee = args.EquipTarget;
    }

    private void OnUnequip(EntityUid uid, ManaRegenComponent component, GotUnequippedEvent args)
    {
        component.Equipee = null;
    }
}
