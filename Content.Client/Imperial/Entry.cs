using Content.Client.Imperial.ShockWave;
using Content.Client.Imperial.Sponsors;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Entry;


public sealed partial class ImperialEntry
{
    public static void Init()
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        prototypeManager.RegisterIgnore("stationGoal");
        prototypeManager.RegisterIgnore("ertCall");
    }

    public static void PostInit()
    {
        var overlayManager = IoCManager.Resolve<IOverlayManager>();

        overlayManager.AddOverlay(new ShockWaveDistortionOverlay());

        IoCManager.Resolve<SponsorsManager>().Initialize();
    }

    public static void IoCRegister()
    {
        IoCManager.Register<SponsorsManager>(); //Imperial sponsors
    }
}
