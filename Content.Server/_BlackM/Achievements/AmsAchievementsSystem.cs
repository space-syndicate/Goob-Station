using Content.Shared._BlackM.Achievements;
using Content.Shared.GameTicking;

namespace Content.Server._BlackM.Achievements;
public sealed class AmsAchievementsSystem : EntitySystem
{
    [Dependency] private readonly AchievementsSystem _achievements = default!;
    private readonly HashSet<EntityUid> _countedParts = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _countedParts.Clear());
    }
    public void OnPartInstalled(EntityUid user, EntityUid part)
    {
        if (!_countedParts.Add(part))
            return;

        _achievements.AddStat(user, AchievementStats.AmsParts);
        EntityManager.System<Content.Server._BlackM.PersonalObjectives.PersonalObjectivesSystem>().AddProgress(user, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.AmsParts); // personal objectives
    }
}
