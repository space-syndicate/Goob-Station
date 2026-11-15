using System.Text;
using Content.Server._BlackM.Ams;
using Content.Server.GameTicking;
using Content.Shared._BlackM.Headcrab;
using Content.Shared._BlackM.PersonalObjectives;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._BlackM.PersonalObjectives;

public sealed class PersonalObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly AmsRoundEndSystem _amsRoundEnd = default!;

    private static readonly ProtoId<NpcFactionPrototype>[] XenFactions = { "BlackMXen" };

    private readonly Dictionary<string, int> _global = new();
    private readonly Dictionary<EntityUid, ProtoId<JobPrototype>> _crew = new();
    private readonly Dictionary<EntityUid, MobState> _lastStates = new();
    private readonly HashSet<EntityUid> _countedKills = new();
    private readonly HashSet<(EntityUid Mind, string Counter, EntityUid Subject)> _unique = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AmsLaunchedEvent>(_ => AddGlobalProgress(BlackMPersonalCounters.AmsLaunched));
        SubscribeLocalEvent<AmsDetonatedEvent>(_ => AddGlobalProgress(BlackMPersonalCounters.AmsDetonated));

        SubscribeLocalEvent<BlackMCounterConditionComponent, ObjectiveGetProgressEvent>(OnCounterProgress);
        SubscribeLocalEvent<BlackMGlobalCounterConditionComponent, ObjectiveGetProgressEvent>(OnGlobalProgress);
        SubscribeLocalEvent<BlackMDepartmentSurvivalConditionComponent, ObjectiveGetProgressEvent>(OnDepartmentProgress);
        SubscribeLocalEvent<BlackMStayHumanConditionComponent, ObjectiveGetProgressEvent>(OnStayHumanProgress);
        SubscribeLocalEvent<BlackMEvacuateConditionComponent, ObjectiveGetProgressEvent>(OnEvacuateProgress);
        SubscribeLocalEvent<BlackMResearchConditionComponent, ObjectiveGetProgressEvent>(OnResearchProgress);
        SubscribeLocalEvent<BlackMBankBalanceConditionComponent, ObjectiveGetProgressEvent>(OnBankProgress);
    }

    #region Public API

    public void AddProgress(EntityUid mob, string counter, int amount = 1)
    {
        if (!_mind.TryGetMind(mob, out var mindId, out _))
            return;

        AddMindProgress(mindId, counter, amount);
    }

    public void AddUniqueProgress(EntityUid mob, string counter, EntityUid subject, int amount = 1)
    {
        if (!_mind.TryGetMind(mob, out var mindId, out _))
            return;

        if (!_unique.Add((mindId, counter, subject)))
            return;

        AddMindProgress(mindId, counter, amount);
    }

    public void AddMindProgress(EntityUid mindId, string counter, int amount = 1)
    {
        var stats = EnsureComp<BlackMPersonalStatsComponent>(mindId);
        stats.Counters[counter] = stats.Counters.GetValueOrDefault(counter) + amount;
    }

    public void AddGlobalProgress(string counter, int amount = 1)
    {
        _global[counter] = _global.GetValueOrDefault(counter) + amount;
    }

    public int GetCounter(EntityUid mindId, string counter)
    {
        return TryComp<BlackMPersonalStatsComponent>(mindId, out var stats)
            ? stats.Counters.GetValueOrDefault(counter)
            : 0;
    }

    public int GetGlobalCounter(string counter)
    {
        return _global.GetValueOrDefault(counter);
    }

    #endregion

    #region Assignment

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId is not { } jobId)
            return;

        if (!_mind.TryGetMind(ev.Mob, out var mindId, out var mind))
            return;

        var job = new ProtoId<JobPrototype>(jobId);
        _crew[mindId] = job;

        BlackMPersonalObjectiveSetPrototype? set = null;
        foreach (var proto in _proto.EnumeratePrototypes<BlackMPersonalObjectiveSetPrototype>())
        {
            if (!proto.Jobs.Contains(job))
                continue;

            set = proto;
            break;
        }

        if (set == null)
            return;

        var stats = EnsureComp<BlackMPersonalStatsComponent>(mindId);
        stats.JobId = jobId;

        foreach (var id in set.Always)
        {
            TryGive(mindId, mind, stats, id);
        }

        var pool = new List<EntProtoId>(set.Pool);
        _random.Shuffle(pool);

        var given = 0;
        foreach (var id in pool)
        {
            if (given >= set.Count)
                break;

            if (TryGive(mindId, mind, stats, id))
                given++;
        }
    }

    private bool TryGive(EntityUid mindId, MindComponent mind, BlackMPersonalStatsComponent stats, EntProtoId id)
    {
        if (!_proto.HasIndex<EntityPrototype>(id.Id))
        {
            Log.Error($"Unknown BlackM personal objective prototype {id.Id}");
            return false;
        }

        if (_objectives.TryCreateObjective(mindId, mind, id.Id) is not { } objective)
            return false;

        _mind.AddObjective(mindId, mind, objective);
        stats.Objectives.Add(objective);
        return true;
    }

    #endregion

    #region Tracking

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (_lastStates.TryGetValue(ev.Target, out var last) && last == ev.NewMobState)
            return;

        _lastStates[ev.Target] = ev.NewMobState;

        if (ev.Origin is not { } rawOrigin)
            return;

        var origin = ResolveActor(rawOrigin);
        if (origin == ev.Target)
            return;

        if (ev.NewMobState == MobState.Dead && IsXenThreat(ev.Target) && _countedKills.Add(ev.Target))
        {
            AddProgress(origin, BlackMPersonalCounters.XenKills);
            return;
        }

        var saved = ev.OldMobState == MobState.Dead && ev.NewMobState != MobState.Dead
                    || ev.OldMobState == MobState.Critical && ev.NewMobState == MobState.Alive;

        if (saved && HasComp<HumanoidAppearanceComponent>(ev.Target) && !IsXenThreat(ev.Target))
            AddUniqueProgress(origin, BlackMPersonalCounters.PatientsSaved, ev.Target);
    }

    private EntityUid ResolveActor(EntityUid origin)
    {
        if (HasComp<MindContainerComponent>(origin))
            return origin;

        var parent = Transform(origin).ParentUid;
        return parent.IsValid() && HasComp<MindContainerComponent>(parent) ? parent : origin;
    }

    private bool IsXenThreat(EntityUid uid)
    {
        return HasComp<ZombieComponent>(uid)
               || HasComp<HeadcrabComponent>(uid)
               || _faction.IsMemberOfAny(uid, XenFactions);
    }

    private bool IsTurned(MindComponent mind)
    {
        return mind.OwnedEntity is { } body
               && (HasComp<ZombieComponent>(body) || HasComp<HeadcrabCapturedComponent>(body));
    }

    private bool IsDeadOrTurned(MindComponent mind)
    {
        return _mind.IsCharacterDeadIc(mind) || IsTurned(mind);
    }

    #endregion

    #region Conditions

    private static float Ratio(int value, int target)
    {
        return target <= 0 ? 1f : Math.Clamp((float) value / target, 0f, 1f);
    }

    private void OnCounterProgress(Entity<BlackMCounterConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.RequireAlive && IsDeadOrTurned(args.Mind))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = Ratio(GetCounter(args.MindId, ent.Comp.Counter), ent.Comp.Target);
    }

    private void OnGlobalProgress(Entity<BlackMGlobalCounterConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.RequireAlive && IsDeadOrTurned(args.Mind))
        {
            args.Progress = 0f;
            return;
        }

        var value = GetGlobalCounter(ent.Comp.Counter);
        args.Progress = ent.Comp.Invert
            ? value == 0 ? 1f : 0f
            : Ratio(value, ent.Comp.Target);
    }

    private void OnDepartmentProgress(Entity<BlackMDepartmentSurvivalConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.RequireAlive && IsDeadOrTurned(args.Mind))
        {
            args.Progress = 0f;
            return;
        }

        HashSet<ProtoId<JobPrototype>>? roles = null;
        if (ent.Comp.Department is { } department && _proto.TryIndex(department, out var departmentProto))
            roles = new HashSet<ProtoId<JobPrototype>>(departmentProto.Roles);

        var total = 0;
        var alive = 0;
        foreach (var (mindId, job) in _crew)
        {
            if (roles != null && !roles.Contains(job))
                continue;

            if (!TryComp<MindComponent>(mindId, out var mind))
                continue;

            total++;
            if (!IsDeadOrTurned(mind))
                alive++;
        }

        if (total == 0 || ent.Comp.Ratio <= 0f)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = Math.Clamp((float) alive / total / ent.Comp.Ratio, 0f, 1f);
    }

    private void OnStayHumanProgress(Entity<BlackMStayHumanConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = IsDeadOrTurned(args.Mind) ? 0f : 1f;
    }

    private void OnEvacuateProgress(Entity<BlackMEvacuateConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = args.Mind.OwnedEntity is { } body
                        && !IsDeadOrTurned(args.Mind)
                        && _amsRoundEnd.IsEvacuated(body)
            ? 1f
            : 0f;
    }

    private void OnResearchProgress(Entity<BlackMResearchConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var best = 0;
        var query = EntityQueryEnumerator<ResearchServerComponent, TechnologyDatabaseComponent>();
        while (query.MoveNext(out _, out _, out var database))
        {
            best = Math.Max(best, database.UnlockedTechnologies.Count);
        }

        args.Progress = Ratio(best, ent.Comp.Technologies);
    }

    private void OnBankProgress(Entity<BlackMBankBalanceConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var best = 0;
        var query = EntityQueryEnumerator<StationBankAccountComponent>();
        while (query.MoveNext(out var uid, out var bank))
        {
            best = Math.Max(best, _cargo.GetBalanceFromAccount((uid, bank), bank.PrimaryAccount));
        }

        args.Progress = Ratio(best, ent.Comp.Balance);
    }

    #endregion

    #region Round flow

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var lines = new List<string>();
        var query = EntityQueryEnumerator<BlackMPersonalStatsComponent, MindComponent>();
        while (query.MoveNext(out var mindId, out var stats, out var mind))
        {
            if (stats.Objectives.Count == 0)
                continue;

            var done = 0;
            var total = 0;
            var sb = new StringBuilder();
            foreach (var objective in stats.Objectives)
            {
                if (TerminatingOrDeleted(objective))
                    continue;

                total++;
                var completed = _objectives.IsCompleted(objective, (mindId, mind));
                if (completed)
                    done++;

                sb.Append('\n');
                sb.Append(Loc.GetString(completed
                        ? "blackm-personal-objectives-roundend-done"
                        : "blackm-personal-objectives-roundend-failed",
                    ("objective", Name(objective))));
            }

            var job = stats.JobId != null && _proto.TryIndex<JobPrototype>(stats.JobId, out var jobProto)
                ? jobProto.LocalizedName
                : "?";

            lines.Add(Loc.GetString("blackm-personal-objectives-roundend-player",
                ("name", mind.CharacterName ?? "?"),
                ("job", job),
                ("done", done),
                ("total", total)) + sb);
        }

        if (lines.Count == 0)
            return;

        ev.AddLine(string.Empty);
        ev.AddLine(Loc.GetString("blackm-personal-objectives-roundend-header"));
        foreach (var line in lines)
        {
            ev.AddLine(line);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _global.Clear();
        _crew.Clear();
        _lastStates.Clear();
        _countedKills.Clear();
        _unique.Clear();
    }

    #endregion
}
