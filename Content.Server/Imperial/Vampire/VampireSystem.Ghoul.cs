using Content.Shared.Body.Components;
using Content.Shared.Imperial.Vampire;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Prayer;
using Content.Server.Bible.Components;
using Content.Shared.Interaction;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Robust.Shared.Player;
using Content.Shared.Mind.Components;

namespace Content.Server.Imperial.Vampire;

public partial class VampireSystem : EntitySystem
{
    private void OnGetDrinkingGhoul(EntityUid uid, GhoulComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || uid == args.Target || !_mobState.IsAlive(args.Target))
            return;

        // если у цели нет крови/разума, кнопки не добавляем
        if (!HasComp<BloodstreamComponent>(args.Target) || !HasComp<MindContainerComponent>(args.Target)
            || !HasComp<ActorComponent>(args.Target))
            return;

        // верб для питья крови
        var verbDrinkBloodGhoul = new InnateVerb
        {
            Act = () =>
            {
                StartDrinking(args.User, args.Target);
            },
            Text = Loc.GetString("vampire-drinking-envelope-text"),
            Message = Loc.GetString("vampire-drinking-envelope-message"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Stellark/Vampire/verbs/convertGhoul.png")),
            Priority = 0
        };
        args.Verbs.Add(verbDrinkBloodGhoul);
    }

    private void OnCureGhoulStart(EntityUid uid, GhoulComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PrayableComponent>(args.Used, out var prayable))
            return;

        if (prayable.BibleUserOnly && !TryComp<BibleUserComponent>(args.User, out _))
            return;

        if (TryComp<VampireComponent>(comp.Vampire, out var vamp))
        {
            if (vamp.VampireTurned)
            {
                _popup.PopupEntity(Loc.GetString("vampire-popup-vampire-turned"),
                args.User, args.User, PopupType.Medium);

                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("vampire-popup-ghoul-rite"),
            args.User, args.User, PopupType.Medium);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.GhoulCure,
            new VampireCureGhoulDoAfterEvent(), args.User, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnCureGhoul(Entity<GhoulComponent> ent, ref VampireCureGhoulDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        RemComp<GhoulComponent>(args.Target.Value);

        // обновляем данные у вампира
        if (TryComp<VampireComponent>(ent.Comp.Vampire, out var vamp))
        {
            if (vamp.Ghouls.Remove(args.Target.Value))
            {
                vamp.GhoulQuantity = Math.Max(0, vamp.GhoulQuantity - 1);
                Dirty(ent.Comp.Vampire, vamp);
            }
        }

        _popup.PopupEntity(Loc.GetString("vampire-popup-successfully-cure-priest"),
            args.User, args.User, PopupType.Medium);

        _popup.PopupEntity(Loc.GetString("vampire-popup-successfully-cure-ghoul"),
            args.Target.Value, args.Target.Value, PopupType.Medium);

        args.Handled = true;
    }

    /// <summary>
    /// обработчик DoAfter для упыря
    /// </summary>
    private void OnDrinkingCompleteGhoul(Entity<GhoulComponent> ent, ref VampireDrinkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        DrinkingComplete(args.User, target, ent.Comp.BloodPerTick);
        args.Handled = true;
    }

    private void ConvertToGhoul(EntityUid vampire, EntityUid target)
    {
        var ghoulComp = EnsureComp<GhoulComponent>(target);
        ghoulComp.Vampire = vampire;

        var masterThralls = EnsureComp<VampireComponent>(vampire);
        masterThralls.Ghouls.Add(target);
        masterThralls.GhoulQuantity++;
        AppealGhoulsCooldown(vampire);

        Dirty(vampire, masterThralls);
        Dirty(target, ghoulComp);

        // добавляем рацию
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(target);
        transmitter.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        transmitter.Channels.Add(new ProtoId<RadioChannelPrototype>(masterThralls.VampireRadioID));

        var activeRadio = EnsureComp<ActiveRadioComponent>(target);
        activeRadio.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        activeRadio.Channels.Add(new ProtoId<RadioChannelPrototype>(masterThralls.VampireRadioID));

        EnsureComp<IntrinsicRadioReceiverComponent>(target);

        _popup.PopupEntity(Loc.GetString("vampire-verb-envelope-vampire-complete",
            ("target", MetaData(target).EntityName)),
            vampire, vampire, PopupType.LargeCaution);

        if (_mind.TryGetMind(target, out var mindId, out var mind))
        {
            // добавление роли и базовых эффектов для игрока
            if (!_roleSystem.MindHasRole<GhoulRoleComponent>(mindId))
                _roleSystem.MindAddRole(mindId, ghoulComp.MindRoleGhoulID, mind: mind);

            if (_player.TryGetSessionById(mind.UserId, out var session))
                _chatMan.DispatchServerMessage(session, Loc.GetString("vampire-verb-envelope-ghoul-greeting"));
        }

        _vampireSystem.SetGhoulBloodAlert(target, ghoulComp);
    }
}
