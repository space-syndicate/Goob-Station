using Content.Shared.Imperial.Restriction.Components;
using Content.Shared.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Whitelist;

namespace Content.Shared.Imperial.Restriction
{
    public sealed class RestrictionSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RestrictionComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        }

        private void OnInteractionAttempt(Entity<RestrictionComponent> ent, ref InteractionAttemptEvent args)
        {
            if (CheckRestriction(ent, ref args))
            {
                if (ent.Comp.Message is { } message)
                {
                    _popup.PopupClient(Loc.GetString(message), args.Uid, args.Uid, PopupType.LargeCaution);
                }

                args.Cancelled = true;
            }
        }

        private bool CheckRestriction(Entity<RestrictionComponent> ent, ref InteractionAttemptEvent args)
        {
            if (args.Cancelled ||
                args.Target == null)
                return false;

            if (!_whitelist.IsWhitelistPass(ent.Comp.Restrictions, args.Target.Value))
                return false;

            return true;
        }
    }
}
