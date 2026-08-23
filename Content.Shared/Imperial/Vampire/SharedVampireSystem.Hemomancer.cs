using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Content.Shared.Physics;
using Content.Shared.Directions;
using Robust.Shared.Physics;


namespace Content.Shared.Imperial.Vampire;

public partial class SharedVampireSystem : EntitySystem
{
    private void HemomancerInitialize()
    {
        SubscribeLocalEvent<VampireTentaclesEvent>(OnTentacles);

        SubscribeLocalEvent<VampireBloodTransformEvent>(OnTransformToBlood);
        SubscribeLocalEvent<VampireBloodTheftEvent>(OnBloodTheft);
    }

    // модифицированный OnSummonAction
    private void OnTentacles(VampireTentaclesEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        var comp = EnsureComp<AbilityComponent>(args.Performer);

        if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (args.Handled)
            return;

        var coords = args.Target;
        List<EntityCoordinates> spawnPos = new();
        spawnPos.Add(coords);

        var dirs = new List<Direction>();
        dirs.AddRange(args.OffsetDirections);

        var extra = Math.Min(args.ExtraSpawns, dirs.Count);
        for (var i = 0; i < extra; i++)
        {
            var dir = _random.PickAndTake(dirs);
            spawnPos.Add(coords.Offset(dir));
        }

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        foreach (var pos in spawnPos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef) ||
                _turf.IsSpace(tileRef) ||
                _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            if (_net.IsServer)
                Spawn(args.EntityId, pos);
        }

        // ссылаемся на VampireTentaclesAction. см VampireBaseAbilities, VampireHemomancer
        if (TryComp<VampireComponent>(args.Performer, out var vampireComponent)) _actions.SetCooldown(vampireComponent.GrantedActions[5], args.CooldownTentacles);
        else if (TryComp<GhoulComponent>(args.Performer, out var ghoulComponent)) _actions.SetCooldown(_entityManager.GetEntity(ghoulComponent.GhoulVampireTentaclesAction), args.CooldownTentacles);
        else return;
        DealAbilityBloodDamageShared(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
    }

    private void OnBloodTheft(VampireBloodTheftEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.Ghouls.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-no-ghouls"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var recover = 0;

        foreach (var ghoulUid in vamp.Ghouls)
        {
            if (!TryComp<GhoulComponent>(ghoulUid, out var ghoul))
                continue;

            if (ghoul.CritThreshold - ghoul.BloodDamage < args.DamageGhoul)
                continue;

            DealGhoulBloodDamage(ghoulUid, args.DamageGhoul, ghoul);

            // восстанавливаем кровь вампиру (по 4 за каждого упыря)
            recover += 4;
        }

        if (recover <= 0)
            _popup.PopupClient(Loc.GetString("vampire-popup-ghouls-no-have-blood"), args.Performer, args.Performer, PopupType.Medium);

        if (recover > 0)
        {
            vamp.BloodDamage = Math.Max(vamp.BloodDamage - recover, 0f);
            SetBloodAlert(args.Performer, vamp);
            Dirty(args.Performer, vamp);

            _popup.PopupClient(Loc.GetString("vampire-popup-stole-blood", ("recover", recover)), args.Performer,
            args.Performer, PopupType.Medium);
        }

        args.Handled = true;
    }

    private void OnTransformToBlood(VampireBloodTransformEvent args)
    {
        if (!TryComp<VampireComponent>(args.Performer, out var vamp) && !TryComp<GhoulComponent>(args.Performer, out var ghoul))
            return;

        var comp = EnsureComp<AbilityComponent>(args.Performer);

        if (!HasEnoughBloodShared(args.Performer, args.CostBlood))
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (comp.BuffBlocked)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-warning-turning-blood"),
            args.Performer, args.Performer, PopupType.Medium);

            return;
        }

        // устанавливаем коллизию такой, чтобы можно было проходить сквозь обьекты
        if (TryComp<FixturesComponent>(args.Performer, out var fixtures))
        {
            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                _physics.SetHard(args.Performer, fixture, false, fixtures);
            }
        }

        VampireInvisible(args.Performer);
        comp.BuffBlockedUntil = _gameTiming.CurTime + args.BloodTime;

        comp.VampireIsBlood = true;

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, comp);
        args.Handled = true;
    }

    private void HemomancerUpdate()
    {
        var bloodVamp = EntityQueryEnumerator<AbilityComponent, FixturesComponent>();
        while (bloodVamp.MoveNext(out var uid, out var comp, out var fixtures))
        {
            if (!TryComp<VampireComponent>(uid, out var vamp) && !TryComp<GhoulComponent>(uid, out var ghoul))
                continue;

            if (!comp.VampireIsBlood)
                continue;

            if (_gameTiming.CurTime >= comp.NextBloodshed)
            {
                SpawnBloodPuddle(uid, comp.VampirePuddleID);
                // оставляем кровавый след за вампиром
                comp.NextBloodshed = _gameTiming.CurTime + TimeSpan.FromSeconds(0.1f);
                Dirty(uid, comp);
            }

            if (_gameTiming.CurTime >= comp.BuffBlockedUntil && comp.VampireIsBlood)
            {
                VampireInvisible(uid);
                comp.VampireIsBlood = false;

                foreach (var (id, fixture) in fixtures.Fixtures)
                {
                    _physics.SetHard(uid, fixture, true, fixtures);
                }

                Dirty(uid, comp);
                continue;
            }
        }
    }
}
