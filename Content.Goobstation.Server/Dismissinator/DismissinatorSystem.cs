// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Dismissinator;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Antag;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Objectives;
using Content.Server.Station.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Dismissinator;

/// <summary>
///     Server half of the "увольнятор": stamps the loadout onto the fired bolt, and on impact strips the
///     victim's ID card and drops a filled, stamped dismissal notice at their feet.
/// </summary>
public sealed class DismissinatorSystem : EntitySystem
{
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedDismissinatorSystem _dismissinator = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DismissinatorComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<DismissalNoticeComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    /// <summary>
    ///     One sheet of paper is spent per bolt that actually leaves the barrel; the bolt carries a snapshot
    ///     of the loadout with it.
    /// </summary>
    private void OnAmmoShot(Entity<DismissinatorComponent> ent, ref AmmoShotEvent args)
    {
        // The syndicate does not leave the recruiter holding evidence. Firing in the emagged mode cooks
        // the gun off in their hands there and then, whatever the bolt goes on to hit or miss - tying it
        // to the impact would let a reflected or stray bolt leave the gun intact. Yield comes from the
        // gun's own Explosive component, so pulling that off the prototype disarms this entirely.
        if (ent.Comp.Mode == DismissinatorMode.Objective)
        {
            // Held items are parented to whoever is holding them.
            var holder = Transform(ent).ParentUid;

            _explosion.TriggerExplosive(ent, user: holder.IsValid() ? holder : null);
        }

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!_dismissinator.TryGetLoadout(ent, out var idCard, out _, out var stamp))
                return;

            // Consume the blank. A miss wastes it, same as any other form you fill in wrong.
            if (!_itemSlots.TryEject(ent, ent.Comp.PaperSlotId, null, out var paper, doAfter: false))
                return;

            QueueDel(paper);

            var notice = EnsureComp<DismissalNoticeComponent>(projectile);
            notice.AuthorizedAccess = _dismissinator.GetAccessTags(idCard.Value);
            notice.Mode = ent.Comp.Mode;
            notice.Document = ent.Comp.Mode switch
            {
                DismissinatorMode.Expansion => ent.Comp.ExpansionDocument,
                DismissinatorMode.Objective => ent.Comp.ObjectiveDocument,
                _ => ent.Comp.DismissalDocument,
            };
            notice.TraitorRule = ent.Comp.TraitorRule;
            notice.HitEffect = ent.Comp.HitEffect;
            notice.StampState = stamp.Value.Comp.StampState;
            notice.Stamp = new StampDisplayInfo
            {
                StampedName = stamp.Value.Comp.StampedName,
                StampedColor = stamp.Value.Comp.StampedColor,
                StampLargeIcon = stamp.Value.Comp.StampLargeIcon,
            };
            notice.AuthorName = idCard.Value.Comp.FullName ?? Loc.GetString("dismissinator-unknown");
            notice.AuthorJob = idCard.Value.Comp.LocalizedJobTitle ?? Loc.GetString("dismissinator-unknown");
        }
    }

    private void OnProjectileHit(Entity<DismissalNoticeComponent> ent, ref ProjectileHitEvent args)
    {
        var target = args.Target;

        if (ent.Comp.HitEffect is { } effect)
            Spawn(effect, Transform(target).Coordinates);

        if (ent.Comp.Mode == DismissinatorMode.Objective)
        {
            ServeDirective(ent, target, args.Shooter);
            return;
        }

        if (!_idCard.TryFindIdCard(target, out var idCard))
            return;

        // An agent ID forges its credentials rather than being issued them, so there is nothing on file
        // to act on. Everything below runs off what the forgery claims instead, and the paperwork reads
        // like any other notice: neither side is told the card was fake.
        var forged = HasComp<AgentIDCardComponent>(idCard);

        var claimed = forged
            ? GetForgedAccess(idCard)
            : (_access.TryGetTags(idCard) ?? Array.Empty<ProtoId<AccessLevelPrototype>>()).ToList();

        var authorized = new HashSet<ProtoId<AccessLevelPrototype>>(ent.Comp.AuthorizedAccess);

        // What the notice reports: clearance taken away, or clearance signed over.
        List<ProtoId<AccessLevelPrototype>> listed;

        if (ent.Comp.Mode == DismissinatorMode.Expansion)
        {
            // You can only sign over clearance you hold yourself, so this needs no rank check.
            listed = authorized.Except(claimed).OrderBy(tag => tag.Id).ToList();

            if (!forged)
                _access.TrySetTags(idCard, claimed.Concat(listed));
        }
        else
        {
            // You cannot dismiss someone cleared for doors you are not, the same rule the ID console
            // enforces on what a privileged card may give or take. So the HoP cannot fire another head,
            // and the captain cannot fire the NT representative.
            if (!authorized.IsSupersetOf(claimed))
            {
                if (args.Shooter is { } shooter)
                    _popup.PopupEntity(Loc.GetString("dismissinator-outranked"), shooter, shooter, PopupType.MediumCaution);

                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Shooter):player} failed to dismiss {ToPrettyString(target):entity}: {ToPrettyString(idCard):entity} holds [{string.Join(", ", claimed.Except(authorized))}], which the authorizing card cannot revoke");

                return;
            }

            listed = claimed;

            if (!forged)
                _access.TrySetTags(idCard, Array.Empty<ProtoId<AccessLevelPrototype>>());
        }

        SpawnNotice(ent, target, idCard, listed);

        var expansion = ent.Comp.Mode == DismissinatorMode.Expansion;

        _popup.PopupEntity(Loc.GetString(expansion ? "dismissinator-expansion-popup" : "dismissinator-hit-popup"),
            target,
            target,
            PopupType.LargeCaution);

        // Admins still see the truth, even though nobody in-game does.
        var action = expansion ? "expanded access for" : "dismissed";

        if (forged)
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.High,
                $"{ToPrettyString(args.Shooter):player} {action} {ToPrettyString(target):entity}, but {ToPrettyString(idCard):entity} is an agent ID: nothing was changed and the notice lists [{string.Join(", ", listed)}] instead");
        }
        else
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.High,
                $"{ToPrettyString(args.Shooter):player} {action} {ToPrettyString(target):entity}: [{string.Join(", ", listed)}] on {ToPrettyString(idCard):entity}");
        }
    }

    /// <summary>
    ///     The access a genuine card bearing the same job icon would carry. Printed on the notice in place
    ///     of an agent ID's real tags, so the document itself gives nothing away.
    /// </summary>
    private List<ProtoId<AccessLevelPrototype>> GetForgedAccess(Entity<IdCardComponent> idCard)
    {
        // Several jobs can share an icon; ordering by id keeps the same one picked every time.
        foreach (var job in _prototype.EnumeratePrototypes<JobPrototype>().OrderBy(job => job.ID))
        {
            if (job.Icon != idCard.Comp.JobIcon)
                continue;

            var tags = new HashSet<ProtoId<AccessLevelPrototype>>(job.Access);

            foreach (var group in job.AccessGroups)
            {
                tags.UnionWith(_prototype.Index(group).Tags);
            }

            return tags.OrderBy(tag => tag.Id).ToList();
        }

        // No job claims that icon, so fall back to whatever the card says it holds.
        return (_access.TryGetTags(idCard) ?? Array.Empty<ProtoId<AccessLevelPrototype>>()).ToList();
    }

    private void SpawnNotice(Entity<DismissalNoticeComponent> ent,
        EntityUid target,
        Entity<IdCardComponent> idCard,
        List<ProtoId<AccessLevelPrototype>> listed)
    {
        var text = ent.Comp.Mode == DismissinatorMode.Expansion
            ? "dismissinator-expansion-document-text"
            : "dismissinator-document-text";

        SpawnPaper(ent,
            target,
            text,
            ("name", idCard.Comp.FullName ?? Name(target)),
            ("job", idCard.Comp.LocalizedJobTitle ?? Loc.GetString("dismissinator-unknown")),
            ("access", FormatAccess(listed)),
            ("authorName", ent.Comp.AuthorName),
            ("authorJob", ent.Comp.AuthorJob));
    }

    /// <summary>
    ///     Emag-only mode: recruits the target into the syndicate outright and serves them the standing
    ///     orders that come with it.
    /// </summary>
    private void ServeDirective(Entity<DismissalNoticeComponent> ent, EntityUid target, EntityUid? shooter)
    {
        // A mindshield blocks the recruitment outright, and says nothing about why.
        if (HasComp<MindShieldComponent>(target))
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(shooter):player} failed to recruit {ToPrettyString(target):entity}: mindshielded");

            return;
        }

        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        // Already working for them; there is nothing left to hand over.
        if (_roles.MindHasRole<TraitorRoleComponent>(mindId))
            return;

        if (mind.UserId is not { } userId || !_player.TryGetSessionById(userId, out var session))
            return;

        // The rule hands out the uplink, codewords and objectives; we only report what it settled on.
        var before = new HashSet<EntityUid>(mind.Objectives);

        _antag.ForceMakeAntag<TraitorRuleComponent>(session, ent.Comp.TraitorRule);

        var objectives = mind.Objectives.Where(objective => !before.Contains(objective)).ToList();

        SpawnPaper(ent,
            target,
            "dismissinator-objective-document-text",
            ("name", Name(target)),
            ("objectives", FormatObjectives(objectives, mindId, mind)));

        _popup.PopupEntity(Loc.GetString("dismissinator-objective-popup"), target, target, PopupType.LargeCaution);

        _adminLogger.Add(LogType.Action,
            LogImpact.Extreme,
            $"{ToPrettyString(shooter):player} recruited {ToPrettyString(target):entity} into {ent.Comp.TraitorRule} with objectives [{string.Join(", ", objectives.Select(o => Name(o)))}]");
    }

    private string FormatObjectives(List<EntityUid> objectives, EntityUid mindId, MindComponent mind)
    {
        if (objectives.Count == 0)
            return Loc.GetString("dismissinator-objectives-none");

        var lines = new List<string>();

        foreach (var objective in objectives)
        {
            var info = _objectives.GetInfo(objective, mindId, mind);

            lines.Add($"⠀[bold]{info?.Title ?? Name(objective)}[/bold]");

            if (!string.IsNullOrWhiteSpace(info?.Description))
                lines.Add(info.Value.Description);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    ///     Drops a stamped, filled-in form at the target's feet. Station and date are filled the same way
    ///     the document printer does it.
    /// </summary>
    private void SpawnPaper(Entity<DismissalNoticeComponent> ent,
        EntityUid target,
        string text,
        params (string, object)[] args)
    {
        var document = Spawn(ent.Comp.Document, Transform(target).Coordinates);

        if (!TryComp<PaperComponent>(document, out var paper))
            return;

        var station = _station.GetOwningStation(target);

        var full = new List<(string, object)>
        {
            ("station", station != null ? Name(station.Value) : Loc.GetString("dismissinator-unknown")),
            ("date", GetStationTime()),
        };
        full.AddRange(args);

        _paper.SetContent((document, paper), Loc.GetString(text, full.ToArray()));

        _paper.TryStamp((document, paper), ent.Comp.Stamp, ent.Comp.StampState);
    }

    private string FormatAccess(List<ProtoId<AccessLevelPrototype>> listed)
    {
        return listed.Count == 0
            ? Loc.GetString("dismissinator-access-none")
            : string.Join(", ", listed.Select(x => x.Id));
    }

    /// <summary>
    ///     Same shift-time + fake-date format the document printer uses.
    /// </summary>
    private string GetStationTime()
    {
        var time = _gameTicker.RoundDuration().ToString("hh\\:mm\\:ss");
        return time + " " + DateTime.Now.AddYears(1000).ToShortDateString();
    }
}
