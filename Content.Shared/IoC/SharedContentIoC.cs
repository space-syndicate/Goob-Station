using Content.Shared.Humanoid.Markings;
using Content.Shared.Imperial.Entry;
using Content.Shared.Localizations;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<MarkingManager, MarkingManager>();
            IoCManager.Register<ContentLocalizationManager, ContentLocalizationManager>();

            ImperialEntry.IoCRegister();
        }
    }
}
