// SPDX-License-Identifier: MIT

using Content.Corvax.Interfaces.Shared; // Aquila Change
using Content.Shared._Aquila.PersonalLoadouts; // Aquila Change
using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Content.Shared.Whitelist;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register(IDependencyCollection deps)
        {
            deps.Register<MarkingManager, MarkingManager>();
            deps.Register<ContentLocalizationManager, ContentLocalizationManager>();
            // Goob: to port EE Interaction Verbs. I hate this.
            deps.Register<EntityWhitelistSystem>();
            deps.Register<TagSystem>();
            deps.Register<ISharedLoadoutsManager, PersonalLoadoutsManager>(); // Aquila Change
        }
    }
}
