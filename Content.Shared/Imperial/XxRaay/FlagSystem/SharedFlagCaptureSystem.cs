using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Localization;

namespace Content.Shared.Imperial.XxRaay.FlagSystem;

/// <summary>
/// Базовая система захвата флагов
/// </summary>
public abstract class SharedFlagCaptureSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlagCaptureComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FlagCaptureComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<FlagCaptureComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetState(EntityUid uid, FlagCaptureComponent component, ref ComponentGetState args)
    {
        args.State = new FlagCaptureComponentState(
            component.CaptureProgress,
            component.IsBeingCaptured);
    }

    private void OnHandleState(EntityUid uid, FlagCaptureComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FlagCaptureComponentState state)
            return;

        component.CaptureProgress = state.CaptureProgress;
        component.IsBeingCaptured = state.IsBeingCaptured;
    }

    private void OnExamined(EntityUid uid, FlagCaptureComponent component, ExaminedEvent args)
    {
        if (component.IsBeingCaptured)
        {
            var captureTimeSeconds = component.CaptureTime.TotalSeconds;
            if (captureTimeSeconds > 0)
            {
                var progressPercent = (int)((component.CaptureProgress.TotalSeconds / captureTimeSeconds) * 100);
                args.PushMarkup($"[color=yellow]{Loc.GetString("flag-capture-examine-progress", ("progress", progressPercent))}[/color]");
            }
            else
            {
                args.PushMarkup($"[color=yellow]{Loc.GetString("flag-capture-examine-capturing")}[/color]");
            }
        }
        else if (component.CanBeCaptured)
        {
            args.PushMarkup($"[color=green]{Loc.GetString("flag-capture-examine-capturable")}[/color]");
        }
    }
}
