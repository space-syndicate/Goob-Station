using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Server.Objectives.Systems;
using Content.Shared.Damage;
using Content.Server.Imperial.NinjaMultitask.Components;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared.Ninja.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Roles.Jobs;
using Content.Shared.Mobs;
namespace Content.Server.Imperial.NinjaMultitask.Systems;

public sealed class DealDamageConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mindManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DealDamageConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<DealDamageConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign, after: new[] { typeof(TargetObjectiveSystem) });
        SubscribeLocalEvent<NinjaDamageTargetComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<NinjaDamageTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnGetProgress(EntityUid uid, DealDamageConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.MinDamage, comp.MaxDamage, comp.DamageDealt, comp.Failed);
    }

    private float GetProgress(EntityUid target, FixedPoint2 mindmg, FixedPoint2 maxdmg, FixedPoint2 dltdmg, bool failed)
    {
        if (failed)
            return 0f;
        if (Math.Clamp((float)(dltdmg), (float)(mindmg), (float)(maxdmg)) == dltdmg)
            {
                return 1f;
            }
            else if (dltdmg < mindmg)
            {
                return (float)(dltdmg / mindmg);
            }
        return 0f;

    }
    private void OnAfterAssign(EntityUid uid, DealDamageConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(uid, out var target) || target == null)
            return;
        var bodyUid = EntityUid.Invalid;
        if (TryComp<MindComponent>(target.Value, out var mindComp) && mindComp.OwnedEntity != null)
        {
            bodyUid = mindComp.OwnedEntity ?? EntityUid.Invalid;
        }
        comp.Ninja = args.Mind.Owner;
        var trgt = target ?? EntityUid.Invalid;
        var targcomp = EnsureComp<NinjaDamageTargetComponent>(bodyUid);
        comp.OriginalBody = args.Mind.OwnedEntity;
        comp.DamageType = _random.Pick(comp.DamageTypePool);
        targcomp.Objective = uid;
        var meta = args.Meta;

        _metaData.SetEntityName(uid, GetTitle(trgt, comp.Title), meta);
    }

    private void OnDamageChanged(EntityUid uid, NinjaDamageTargetComponent component, DamageChangedEvent args)
    {
        if (component.Objective == null)
        {
            return;
        }
        if (!TryComp<DealDamageConditionComponent>(component.Objective.Value, out var comp) ||
                comp.DamageType == null)
        {
            return;
        }
        if (args.DamageDelta == null)
        {
            return;
        }
        var damageType = comp.DamageType.Value;
        var org = args.Origin ?? EntityUid.Invalid;
        if (!args.DamageDelta.DamageDict.TryGetValue(damageType, out var damageDelta))
        {
            return;
        }
        if (comp.DealDamageYourself == true)
        {
            var org1 = EntityUid.Invalid;
            ICommonSession session = default!;
            if (!TryComp<ActorComponent>(args.Origin, out var actor))
            {
                return;
            }
            session = actor.PlayerSession;
            if (!_mindManager.TryGetMind(session, out var mindIdNinja, out var mindComponentNinja))
                return;
            if (mindComponentNinja.Owner == comp.Ninja && args.Origin == comp.OriginalBody && args.DamageIncreased && damageDelta > 0)
            {
                comp.DamageDealt += damageDelta;
            }
        }
        else
        {
            comp.DamageDealt += damageDelta;
        }
    }
    private string GetTitle(EntityUid target, string title)
    {
        var targetName = "Unknown";
        var ownedEntity = EntityUid.Invalid;
        if (TryComp<MindComponent>(target, out var mind) && mind.CharacterName != null)
        {
            ownedEntity = mind.OwnedEntity ?? EntityUid.Invalid;
            targetName = mind.CharacterName;
        }
        if (string.IsNullOrEmpty(title))
            return string.Empty;
        if (ownedEntity == null || !TryComp<NinjaDamageTargetComponent>(ownedEntity, out var compninjatarget))
        {
            return string.Empty;
        }
        if (compninjatarget.Objective == null) // Добавлена проверка
            return string.Empty;
        if (!TryComp<DealDamageConditionComponent>(compninjatarget.Objective.Value, out var comp) || comp.DamageType == null)
        {
            return string.Empty;
        }
        var mindmg = comp.MinDamage.ToString();
        var maxdmg = comp.MaxDamage.ToString();
        var damageTypeProto = new DamageTypePrototype();
        if (_prototype.TryIndex<DamageTypePrototype>(comp.DamageType, out var proto))
        {
            damageTypeProto = proto;
        }
        var type = damageTypeProto?.LocalizedName ?? comp.DamageType.Value;
        var jobName = "Unknown";
        if (TryComp<MindComponent>(target, out var mindComp))
        {
            jobName = _job.MindTryGetJobName(target) ?? jobName;
        }
        return Loc.GetString(title,
            ("mindmg", mindmg),
            ("maxdmg", maxdmg),
            ("dmgtype", type),
            ("job", jobName),
            ("targetName", targetName));
    }
    private void OnMobStateChanged(EntityUid uid, NinjaDamageTargetComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
        {
            if (component.Objective == null)
            {
                return;
            }
            if (!TryComp<DealDamageConditionComponent>(component.Objective.Value, out var comp) ||
                    comp.DamageType == null)
            {
                return;
            }
            if (!TryComp<MindContainerComponent>(component.Owner, out var mccomp))
            {
                return;
            }
            if (!TryComp<MindComponent>(mccomp.Mind, out var mcomp))
            {
                return;
            }
            if (!TryComp<DamageableComponent>(mcomp.OwnedEntity, out var damagecomp))
            {
                return;
            }
            var damageType = comp.DamageType.Value;
            if (!damagecomp.Damage.DamageDict.TryGetValue(damageType, out var damageDelta))
            {
                return;
            }
            if (damageDelta >= 100)
            {
                comp.Failed = true;
            }
        }
    }
}
