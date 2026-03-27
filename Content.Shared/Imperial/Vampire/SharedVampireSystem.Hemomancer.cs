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
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
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
        _actions.SetCooldown(vamp.GrantedActions[4], args.CooldownTentacles);
        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, vamp);
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

            // восстанавливаем кровь вампиру (по 2 за каждого упыря)
            recover += 2;
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
        if (!TryComp<VampireComponent>(args.Performer, out var vamp))
            return;

        if (vamp.BloodDamage + args.CostBlood >= vamp.CritThreshold)
        {
            _popup.PopupClient(Loc.GetString("vampire-popup-not-enough-blood"),
                args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (vamp.BuffBlocked)
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
        vamp.BuffBlockedUntil = _gameTiming.CurTime + args.BloodTime;

        vamp.VampireIsBlood = true;

        DealBloodDamage(args.Performer, args.CostBlood);
        Dirty(args.Performer, vamp);
        args.Handled = true;
    }

    private void HemomancerUpdate()
    {
        var bloodVamp = EntityQueryEnumerator<VampireComponent, FixturesComponent>();
        while (bloodVamp.MoveNext(out var uid, out var vamp, out var fixtures))
        {
            if (!vamp.VampireIsBlood)
                continue;

            if (_gameTiming.CurTime >= vamp.NextBloodshed)
            {
                SpawnBloodPuddle(uid, vamp.VampirePuddleID);
                // оставляем кровавый след за вампиром
                vamp.NextBloodshed = _gameTiming.CurTime + TimeSpan.FromSeconds(0.1f);
                Dirty(uid, vamp);
            }

            if (_gameTiming.CurTime >= vamp.BuffBlockedUntil && vamp.VampireIsBlood)
            {
                VampireInvisible(uid);
                vamp.VampireIsBlood = false;

                foreach (var (id, fixture) in fixtures.Fixtures)
                {
                    _physics.SetHard(uid, fixture, true, fixtures);
                }

                Dirty(uid, vamp);
                continue;
            }
        }
    }
}
