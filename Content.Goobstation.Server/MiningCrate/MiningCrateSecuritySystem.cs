// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Explosion.EntitySystems;
using Content.Goobstation.Shared.MiningCrate;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion.Components;
using Content.Shared.Lock;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.MiningCrate;

public sealed class MiningCrateSecuritySystem : SharedMiningCrateSecuritySystem
{
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly MiningCrateSystem _crate = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MiningCrateSecurityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MiningCrateSecurityComponent, DamageThresholdReached>(OnDamageThreshold);
        SubscribeLocalEvent<MiningCrateSecurityComponent, AttemptChangePanelEvent>(OnAttemptChangePanel);
    }

    private void OnMapInit(Entity<MiningCrateSecurityComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.IntensityCaptured)
            return;

        if (TryComp<ExplosiveComponent>(ent, out var explosive))
        {
            ent.Comp.StoredTotalIntensity = explosive.TotalIntensity;
            ent.Comp.IntensityCaptured = true;
            Dirty(ent);
        }
    }

    private void OnDamageThreshold(Entity<MiningCrateSecurityComponent> ent, ref DamageThresholdReached args)
    {
        if (!ent.Comp.Armed && !ent.Comp.Detonating)
            return;

        TriggerBlast(ent);
    }

    private void OnAttemptChangePanel(Entity<MiningCrateSecurityComponent> ent, ref AttemptChangePanelEvent args)
    {
        if (args.Cancelled)
            return;

        if (!IsMidDeconstruction(ent))
            return;

        args.Cancelled = true;
    }

    private bool IsMidDeconstruction(EntityUid uid)
    {
        return TryComp<ConstructionComponent>(uid, out var construction) && construction.EdgeIndex != null;
    }

    public bool SetArmed(EntityUid uid, bool armed, EntityUid? user = null)
    {
        if (!TryComp<MiningCrateSecurityComponent>(uid, out var comp))
            return false;

        if (comp.Detonating)
            return true;

        comp.Armed = armed;
        Dirty(uid, comp);

        if (user != null)
        {
            Popup.PopupEntity(
                Loc.GetString(armed
                    ? "lavaland-mining-crate-security-rearmed"
                    : "lavaland-mining-crate-security-disarmed"),
                uid,
                user.Value);
        }

        return true;
    }

    public bool SetLockWireCut(EntityUid uid, bool cut, EntityUid? user = null)
    {
        if (!TryComp<MiningCrateSecurityComponent>(uid, out var comp))
            return false;

        comp.LockWireCut = cut;
        Dirty(uid, comp);

        if (cut && TryComp<LockComponent>(uid, out var lockComp) && !lockComp.Locked)
            _lock.Lock(uid, user, lockComp);

        if (user != null)
        {
            Popup.PopupEntity(
                Loc.GetString(cut
                    ? "lavaland-mining-crate-security-lock-wire-cut"
                    : "lavaland-mining-crate-security-lock-wire-mended"),
                uid,
                user.Value);
        }

        return true;
    }

    public bool SetSirenWireIntact(EntityUid uid, bool intact, EntityUid? user = null)
    {
        if (!TryComp<MiningCrateSecurityComponent>(uid, out var comp))
            return false;

        if (comp.SirenWireIntact == intact)
        {
            _crate.SyncPhysicalSiren(uid);
            return true;
        }

        comp.SirenWireIntact = intact;
        Dirty(uid, comp);
        _crate.SyncPhysicalSiren(uid);

        if (user != null)
        {
            Popup.PopupEntity(
                Loc.GetString(intact
                    ? "lavaland-mining-crate-security-siren-mended"
                    : "lavaland-mining-crate-security-siren-cut"),
                uid,
                user.Value);
        }

        return true;
    }

    public bool OnBoomWireCut(EntityUid uid, EntityUid user)
    {
        if (!TryComp<MiningCrateSecurityComponent>(uid, out var comp))
            return true;

        if (!comp.Armed)
        {
            Popup.PopupEntity(Loc.GetString("lavaland-mining-crate-security-boom-disarmed"), uid, user);
            return true;
        }

        StartDetonation((uid, comp), user);
        return true;
    }

    public void OnBoomWirePulse(EntityUid uid, EntityUid user)
    {
        if (!TryComp<MiningCrateSecurityComponent>(uid, out var comp))
            return;

        if (!comp.Armed)
        {
            Popup.PopupEntity(Loc.GetString("lavaland-mining-crate-security-pulse-disarmed"), uid, user);
            return;
        }

        StartDetonation((uid, comp), user);
    }

    public void StartDetonation(Entity<MiningCrateSecurityComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Detonating || !ent.Comp.Armed)
            return;

        ent.Comp.Detonating = true;
        ent.Comp.DetonateAt = _timing.CurTime + ent.Comp.DetonateDelay;
        Dirty(ent);

        _crate.SyncPhysicalSiren(ent.Owner);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/alarm.ogg"), ent);
        if (user != null)
            Popup.PopupEntity(Loc.GetString("lavaland-mining-crate-security-detonating"), ent, user.Value);
        else
            Popup.PopupEntity(Loc.GetString("lavaland-mining-crate-security-detonating"), ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MiningCrateSecurityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Detonating)
                continue;

            if (_timing.CurTime < comp.DetonateAt)
                continue;

            FinishDetonation((uid, comp));
        }
    }

    private void FinishDetonation(Entity<MiningCrateSecurityComponent> ent)
    {
        ent.Comp.Detonating = false;
        Dirty(ent);

        _crate.SyncPhysicalSiren(ent.Owner);

        var damage = new DamageSpecifier(
            _prototypes.Index(BluntDamage),
            FixedPoint2.New(ent.Comp.DetonateDamage));

        _damageable.TryChangeDamage(ent.Owner, damage, ignoreResistances: true, interruptsDoAfters: false);
    }

    private void TriggerBlast(Entity<MiningCrateSecurityComponent> ent)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosive))
            return;

        var intensity = ent.Comp.StoredTotalIntensity > 0
            ? ent.Comp.StoredTotalIntensity
            : explosive.TotalIntensity;

        _explosion.QueueExplosion(
            _transform.GetMapCoordinates(ent.Owner),
            explosive.ExplosionType.Id,
            intensity,
            explosive.IntensitySlope,
            explosive.MaxIntensity,
            ent.Owner,
            explosive.TileBreakScale,
            explosive.MaxTileBreak,
            explosive.CanCreateVacuum);
    }
}
