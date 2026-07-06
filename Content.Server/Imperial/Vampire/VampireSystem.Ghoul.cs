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
using Content.Shared.Actions.Components;

namespace Content.Server.Imperial.Vampire;

public partial class VampireSystem : EntitySystem
{
    private void OnGetDrinkingGhoul(EntityUid uid, GhoulComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || uid == args.Target || !_mobState.IsAlive(args.Target))
            return;

        // верб для питья крови
        if (!HasComp<GhoulComponent>(args.Target) && !HasComp<VampireComponent>(args.Target) && HasComp<BloodstreamComponent>(args.Target))
        {
            var verbDrinkBloodGhoul = new InnateVerb
            {
                Act = () =>
                {
                    StartDrinking(args.User, args.Target);
                },
                Text = Loc.GetString("vampire-drinking-envelope-text"),
                Message = Loc.GetString("vampire-drinking-envelope-message"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Stellark/Vampire/verbs/drinkBlood.png")),
                Priority = 0
            };
            args.Verbs.Add(verbDrinkBloodGhoul);
        }
    }

    private void OnCureGhoulStart(EntityUid uid, GhoulComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PrayableComponent>(args.Used, out var prayable))
            return;

        if (prayable.BibleUserOnly && !TryComp<BibleUserComponent>(args.User, out _))
            return;

        if (TryComp<VampireComponent>(comp.Vampire, out var vamp) && TryComp<AbilityComponent>(comp.Vampire, out var abilityComponent))
        {
            if (abilityComponent.VampireTurned)
            {
                _popup.PopupEntity(Loc.GetString("vampire-popup-vampire-turned"),
                args.User, args.User, PopupType.Medium);

                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("vampire-popup-ghoul-rite"),
            args.User, args.User, PopupType.Medium);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.GhoulCure,
            new VampireCureGhoulDoAfterEvent(), uid, target: uid)
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

        RemoveMindFromGhoul(ent);
        RemComp<GhoulComponent>(args.Target.Value);
        _vampireSystem.SetGhoulBloodAlert(ent, ent.Comp);

        // обновляем данные у вампира
        if (TryComp<VampireComponent>(ent.Comp.Vampire, out var vamp) && TryComp<AbilityComponent>(ent.Comp.Vampire, out var abilityComponent))
        {
            var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(args.Target.Value);
            transmitter.Channels.Remove(new ProtoId<RadioChannelPrototype>(abilityComponent.VampireRadioID));

            var activeRadio = EnsureComp<ActiveRadioComponent>(args.Target.Value);
            activeRadio.Channels.Remove(new ProtoId<RadioChannelPrototype>(abilityComponent.VampireRadioID));

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

        EnsureComp<VampireJerkComponent>(target);
        EnsureComp<AbilityComponent>(target);

        var masterThralls = EnsureComp<VampireComponent>(vampire);
        var abilityComponent = EnsureComp<AbilityComponent>(vampire);
        masterThralls.Ghouls.Add(target);
        masterThralls.GhoulQuantity++;
        AppealGhoulsCooldown(vampire);

        Dirty(vampire, masterThralls);
        Dirty(target, ghoulComp);

        // добавляем рацию
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(target);
        transmitter.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        transmitter.Channels.Add(new ProtoId<RadioChannelPrototype>(abilityComponent.VampireRadioID));

        var activeRadio = EnsureComp<ActiveRadioComponent>(target);
        activeRadio.Channels ??= new HashSet<ProtoId<RadioChannelPrototype>>();
        activeRadio.Channels.Add(new ProtoId<RadioChannelPrototype>(abilityComponent.VampireRadioID));

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

        if (abilityComponent.VampireTurned)
        {
            var ghoulAbilityComp = EnsureComp<AbilityComponent>(target);
            EnsureComp<ActionsComponent>(target);

            ghoulAbilityComp.HaloUid = Spawn(ghoulAbilityComp.HaloEffect, Transform(target).Coordinates);
            _transform.SetParent(ghoulAbilityComp.HaloUid.Value, target);

            var usedIndices = new HashSet<int>();
            for (int i = 0; i < abilityComponent.GhoulBaseAbility; i++)
            {
                int abilityNumber;
                do
                {
                    abilityNumber = _random.Next(0, 3);
                } while (usedIndices.Contains(abilityNumber) || abilityNumber == 2);

                usedIndices.Add(abilityNumber);

                if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                    masterThralls.VampireAbilitiesID[VampireAbilityType.Base], out var baseAbilities))
                {
                    var action = _actions.AddAction(target, baseAbilities.Abilities[abilityNumber]);
                    if (action != null)
                    {
                        // см BaseAbilities. Ссылаемся на "Кровавая катана"
                        if (abilityNumber == 0) ghoulComp.GhoulVampireSwordAction = _entityManager.GetNetEntity(action);
                        ghoulComp.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                    }
                }
            }

            for (int i = 0; i < abilityComponent.GhoulGroupAbility; i++)
            {
                var abilityNumber = _random.Next(0, 3);

                switch (masterThralls.SelectedSubgroup)
                {
                    case VampireAbilityType.Hemomancer:
                        if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                            masterThralls.VampireAbilitiesID[VampireAbilityType.Hemomancer], out var hemomancerAbilities))
                        {
                            var action = _actions.AddAction(target, hemomancerAbilities.Abilities[abilityNumber]);
                            // см BaseAbilities. Ссылаемся на "Кровавые щупальца"
                            if (abilityNumber == 5) ghoulComp.GhoulVampireTentaclesAction = _entityManager.GetNetEntity(action);
                            if (action != null) ghoulComp.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                        }

                        break;

                    case VampireAbilityType.Umbrae:
                        if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                            masterThralls.VampireAbilitiesID[VampireAbilityType.Umbrae], out var umbraeAbilities))
                        {
                            var action = _actions.AddAction(target, umbraeAbilities.Abilities[abilityNumber]);
                            // см BaseAbilities. Ссылаемся на "Кровавый якорь"
                            if (abilityNumber == 7) ghoulComp.GhoulVampireBloodAnchorAction = _entityManager.GetNetEntity(action);
                            if (action != null) ghoulComp.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                        }

                        break;

                    case VampireAbilityType.Gargantua:
                        if (_prototypeManager.TryIndex<VampireAbilityListPrototype>(
                            masterThralls.VampireAbilitiesID[VampireAbilityType.Gargantua], out var gargantuaAbilities))
                        {
                            var action = _actions.AddAction(target, gargantuaAbilities.Abilities[abilityNumber]);
                            if (action != null) ghoulComp.GhoulGrantedActions.Add(_entityManager.GetNetEntity(action.Value));
                        }

                        break;
                }
            }

            Dirty(target, ghoulComp);
        }

        _vampireSystem.SetGhoulBloodAlert(target, ghoulComp);
    }

    private void RemoveMindFromGhoul(EntityUid uid)
    {
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
        {
            _roleSystem.MindRemoveRole<GhoulRoleComponent>((mindId, mind));
        }

        if (!TryComp<GhoulComponent>(uid, out var ghoulComponent)) return;

        var comp = EnsureComp<AbilityComponent>(uid);
        if (comp.HaloUid != null) QueueDel(comp.HaloUid);
        if (comp.InvisibleIsActive) _vampireSystem.VampireInvisible(uid);
        if (comp.ItemIssued) _vampireSystem.OnIssuingSword(uid);

        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        transmitter.Channels.Remove(new ProtoId<RadioChannelPrototype>(comp.VampireRadioID));

        var activeRadio = EnsureComp<ActiveRadioComponent>(uid);
        activeRadio.Channels.Remove(new ProtoId<RadioChannelPrototype>(comp.VampireRadioID));

        if (ghoulComponent.GhoulGrantedActions.Count > 0)
        {
            foreach (var action in ghoulComponent.GhoulGrantedActions)
            {
                _actions.RemoveAction(uid, _entityManager.GetEntity(action));
            }
        }

        RemComp<GhoulComponent>(uid);

        _alert.ClearAlert(uid, comp.AdjacentChaplainAlert);
        _vampireSystem.SetGhoulBloodAlert(uid, ghoulComponent);

        RemComp<AbilityComponent>(uid);
    }
}
