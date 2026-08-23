using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.XxRaay.Components;

[RegisterComponent]
public sealed partial class ImperialVentCrawlerComponent : Component
{
    [DataField(required: true)]
    public TimeSpan EnterDelay;

    [DataField(required: true)]
    public TimeSpan ExitDelay;

    [DataField(required: true)]
    public float VentSpeedMultiplier;

    [DataField]
    public SoundSpecifier? MoveSound;

    [DataField]
    public float MoveSoundInterval;

    [DataField(required: true)]
    public LocId EnterVerb;

    [DataField(required: true)]
    public LocId ExitVerb;

    [DataField(required: true)]
    public SpriteSpecifier EnterVerbIcon;

    [DataField(required: true)]
    public SpriteSpecifier ExitVerbIcon;

    [DataField]
    public int EnterVerbPriority;

    [DataField]
    public int ExitVerbPriority;

    [DataField]
    public bool UseStealth;

    [DataField]
    public float StealthVisibility;

    [DataField]
    public bool DisableActions;

    [DataField]
    public bool RemoveComplexInteraction;

    [DataField]
    public bool RevealPipeNetwork;

    [DataField]
    public float PipeRevealRange;

    [DataField]
    public ushort HiddenVisibilityLayer;

    [DataField]
    public ushort CrawlVisibilityLayer;

    [DataField]
    public bool EyeSeeSubfloor;

    [DataField]
    public bool EyeSeeCrawlVisibilityLayer;
}
