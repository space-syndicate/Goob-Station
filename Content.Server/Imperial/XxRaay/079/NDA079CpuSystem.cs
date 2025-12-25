using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Imperial.XxRaay.Nda079;
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

    private async void SendReport(EntityUid target, int newLevel)
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

        for (var percent = 0; percent <= 100; percent += 10)
        {
            await Timer.Delay(500);
            if (session.Status != SessionStatus.InGame)
                break;

            var msg = Loc.GetString("nda079-cpu-hacking", ("percent", percent));
            _chatManager.ChatMessageToOne(
                ChatChannel.Server,
                msg,
                msg,
                EntityUid.Invalid,
                false,
                session.Channel,
                Color.Red
            );
        }

        await Timer.Delay(500);

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
    }

}


