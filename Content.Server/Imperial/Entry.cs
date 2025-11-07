using Content.Server.Imperial.Sponsors;

namespace Content.Server.Imperial.Entry;


public sealed partial class ImperialEntry
{
    public static void Init()
    {
        IoCManager.Resolve<SponsorsManager>().Initialize();
    }

    public static void PostInit()
    {

    }

    public static void IoCRegister()
    {
        IoCManager.Register<SponsorsManager>();
    }
}
