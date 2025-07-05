using Robust.Shared.Audio;
using Content.Server.RoundEnd;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking;
using Content.Server.Imperial.PiratesNewHorizon.Rules.Systems;
namespace Content.Server.Imperial.PiratesNewHorizon.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]
public sealed partial class PiratesRuleComponent : Component
{
    [ViewVariables]
    public EntityUid PirateShuttle = EntityUid.Invalid;
    [DataField]
    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;
    [ViewVariables]
    public HashSet<EntityUid> InitialItems = new();
    [ViewVariables]
    public double InitialShipValue;

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("pirateAlertSound")]
    public SoundSpecifier PirateAlertSound = new SoundPathSpecifier(
        "/Audio/Ambience/Antag/pirate_start.ogg",
        AudioParams.Default.WithVolume(4));
    
    /// <summary
    /// Amount of cash required for pirates to win
    /// </summary>
    
    [DataField("winningScore")]
    public int WinningScore = 300000;
    /// <summary>
    /// Text for shuttle call if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    /// <summary>
    /// Text for shuttle call if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public string RoundEndTextShuttleCall = "pirates-no-more-threat-announcement-shuttle-call";

    /// <summary>
    /// Text for announcement if RoundEndBehavior is ShuttleCall. Used if shuttle is already called
    /// </summary>
    [DataField]
    public string RoundEndTextAnnouncement = "pirates-no-more-threat-announcement";

    /// <summary>
    /// Time to emergency shuttle to arrive if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(3);
}