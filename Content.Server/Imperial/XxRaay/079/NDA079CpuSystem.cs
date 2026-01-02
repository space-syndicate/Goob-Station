using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Imperial.XxRaay.Nda079.Components;
using Content.Shared.Imperial.XxRaay.Nda079.Events;
using Content.Shared.Imperial.XxRaay.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.XxRaay.Nda079;

/// <summary>
/// Система, считающая CPU поинты 079 и повышающая уровень.
/// </summary>
public sealed class NDA079CpuSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private const int MessageIntervalMs = 500;
    private const int PercentStep = 10;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NDA079CpuHackingComponent>();
        var curTime = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out var hacking))
        {
            if (curTime < hacking.NextMessageTime)
                continue;

            if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            {
                RemComp<NDA079CpuHackingComponent>(uid);
                continue;
            }

            if (session.Status != SessionStatus.InGame)
            {
                RemComp<NDA079CpuHackingComponent>(uid);
                continue;
            }

            if (hacking.CurrentPercent > 100)
            {
                var endText = Loc.GetString("nda079-cpu-endtext");
                _chatManager.ChatMessageToOne(
                    ChatChannel.Server,
                    endText,
                    endText,
                    EntityUid.Invalid,
                    false,
                    session.Channel,
                    Color.Red
                );
                RemComp<NDA079CpuHackingComponent>(uid);
                continue;
            }

            var msg = Loc.GetString("nda079-cpu-hacking", ("percent", hacking.CurrentPercent));
            _chatManager.ChatMessageToOne(
                ChatChannel.Server,
                msg,
                msg,
                EntityUid.Invalid,
                false,
                session.Channel,
                Color.Red
            );

            hacking.CurrentPercent += PercentStep;
            hacking.NextMessageTime = curTime + TimeSpan.FromMilliseconds(MessageIntervalMs);
            Dirty(uid, hacking);
        }
    }

    public void AddCpuPoint(EntityUid user)
    {
        if (!TryComp<NDA079CpuComponent>(user, out var cpu))
            return;

        if (cpu.CurrentLevel >= cpu.LvlMax)
            return;

        cpu.CurrentCpu += 1;
        Dirty(user, cpu);

        CheckLevelUp(user, cpu);
    }

    private void CheckLevelUp(EntityUid user, NDA079CpuComponent cpu)
    {
        var currentLevel = cpu.CurrentLevel;
        if (currentLevel >= cpu.LvlMax)
            return;

        if (currentLevel < 1 || currentLevel >= cpu.LevelRequirements.Length)
            return;

        var required = cpu.LevelRequirements[currentLevel];
        if (cpu.CurrentCpu < required)
            return;

        cpu.CurrentCpu = 0;
        cpu.CurrentLevel += 1;
        Dirty(user, cpu);

        HandleLevelUp(user, cpu.CurrentLevel);
    }

    private void HandleLevelUp(EntityUid user, int newLevel)
    {
        ForceToStationaryAndReport(user, newLevel);
    }

    private void ForceToStationaryAndReport(EntityUid user, int newLevel)
    {
        if (!TryComp<NDA079Component>(user, out var ndaComp))
        {
            SendReport(user, newLevel);
            return;
        }

        if (!ndaComp.InAIVisionMode)
        {
            SendReport(user, newLevel);
            return;
        }

        var target = user;

        if (ndaComp.OriginalEntity is { } original && Exists(original))
            target = original;

        var ev = new NDA079ToggleVisionModeEvent();
        RaiseLocalEvent(user, ev);

        SendReport(target, newLevel);
    }

    private void SendReport(EntityUid target, int newLevel)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var session))
            return;

        var startText = Loc.GetString("nda079-cpu-starttext", ("newLevel", newLevel));
        _chatManager.ChatMessageToOne(
            ChatChannel.Server,
            startText,
            startText,
            EntityUid.Invalid,
            false,
            session.Channel,
            Color.Red
        );

        // Start the hacking sequence using a component-based approach
        var hacking = EnsureComp<NDA079CpuHackingComponent>(target);
        hacking.CurrentPercent = 0;
        hacking.NextMessageTime = _gameTiming.CurTime + TimeSpan.FromMilliseconds(MessageIntervalMs);
        hacking.TargetLevel = newLevel;
        Dirty(target, hacking);
    }

}


