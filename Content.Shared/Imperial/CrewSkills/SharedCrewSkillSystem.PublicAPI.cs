using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.CrewSkills;


public abstract partial class SharedCrewSkillsSystem
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="skills"></param>
    /// <returns></returns>
    public bool TryGetEntityFakeSkills(EntityUid uid, [NotNullWhen(true)] out HashSet<ProtoId<SkillPrototype>>? fakeSkills)
    {
        fakeSkills = null;
        return false;
    }

    /// <summary>
    /// Get entity total skills
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="skills"></param>
    /// <returns></returns>
    public bool TryGetEntitySkills(EntityUid uid, [NotNullWhen(true)] out HashSet<ProtoId<SkillPrototype>>? skills)
    {
        skills = null;
        return false;
    }

    /// <summary>
    /// Get entity missing skills
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="skills"></param>
    /// <returns></returns>
    public bool TryGetEntityMissingSkills(EntityUid uid, [NotNullWhen(true)] out HashSet<ProtoId<SkillPrototype>>? missingSkills)
    {
        missingSkills = null;
        return false;
    }

    /// <summary>
    /// Returns entity total skills if it have skills
    /// </summary>
    /// <param name="uid">Entity or mind</param>
    /// <param name="skills"></param>
    /// <returns></returns>
    public bool TryGetEntitySkillsComponents(EntityUid uid, out MindSkillsComponent? mindSkills, out BodySkillsComponent? bodySkills)
    {
        mindSkills = null;
        bodySkills = null;
        return false;
    }

    /// <summary>
    /// Return mind skills component if it exists
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="mindSkills"></param>
    /// <returns></returns>
    public bool TryGetMindSkills(EntityUid uid, [NotNullWhen(true)] out MindSkillsComponent? mindSkills)
    {
        mindSkills = null;
        return false;
    }

    /// <summary>
    /// Return true if entity have skill
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public bool EntityHaveSkill(EntityUid uid, ProtoId<SkillPrototype> skill)
    {
        return false;
    }

    #region Add

    /// <summary>
    /// Adds skill to entity
    /// </summary>
    /// <param name="uid">Mind or entity</param>
    /// <param name="skillId">Skill</param>
    public bool AddSkillToMindAttempt(EntityUid uid, ProtoId<SkillPrototype> skillId)
    {
        return false;
    }

    /// <summary>
    /// Adds skill group to entity
    /// </summary>
    /// <param name="uid">Mind or entity</param>
    /// <param name="skillId">Skill</param>
    public bool AddGroupToMindAttempt(EntityUid uid, ProtoId<SkillGroupPrototype> groupId)
    {
        return false;
    }

    /// <summary>
    /// Remove skill from entity
    /// </summary>
    /// <param name="uid">Mind or entity</param>
    /// <param name="skillId">Skill</param>
    public bool RemoveSkillFromMindAttempt(EntityUid uid, ProtoId<SkillPrototype> skillId)
    {
        return false;
    }

    /// <summary>
    /// Remove skill group from entity
    /// </summary>
    /// <param name="uid">Mind or entity</param>
    /// <param name="skillId">Skill</param>
    public bool RemoveGroupFromMindAttempt(EntityUid uid, ProtoId<SkillGroupPrototype> groupId)
    {
        return false;
    }

    #endregion

    #region Remove

    /// <summary>
    /// Add skill to body
    /// </summary>
    /// <param name="uid">Entity UID. NOT mind uid</param>
    /// <param name="skillId"></param>
    public bool AddSkillToBodyAttempt(EntityUid uid, ProtoId<SkillPrototype> skillId)
    {
        return false;
    }

    /// <summary>
    /// Add skill group to body
    /// </summary>
    /// <param name="uid">Entity UID. NOT mind uid</param>
    /// <param name="skillId"></param>
    public bool AddGroupToBodyAttempt(EntityUid uid, ProtoId<SkillGroupPrototype> groupId)
    {
        return false;
    }

    /// <summary>
    /// Remove skill from body
    /// </summary>
    /// <param name="uid">Entity UID. NOT mind uid</param>
    /// <param name="skillId"></param>
    public bool RemoveSkillFromBodyAttempt(EntityUid uid, ProtoId<SkillPrototype> skillId)
    {
        return false;
    }

    /// <summary>
    /// Remove skill group from body
    /// </summary>
    /// <param name="uid">Entity UID. NOT mind uid</param>
    /// <param name="skillId"></param>
    public bool RemoveGroupFromBodyAttempt(EntityUid uid, ProtoId<SkillGroupPrototype> groupId)
    {
        return false;
    }

    #endregion
}
