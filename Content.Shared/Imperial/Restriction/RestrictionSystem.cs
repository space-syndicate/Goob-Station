using Content.Shared.Imperial.Restriction.Components;
using Content.Shared.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Roles;
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
                if (ent.Comp.Message is not { } message)
                    return;

                args.Cancelled = true;
                _popup.PopupClient(Loc.GetString(message), args.Uid, args.Uid, PopupType.LargeCaution);
            }
        }

        private bool CheckRestriction(Entity<RestrictionComponent> ent, ref InteractionAttemptEvent args)
        {
            if (args.Cancelled ||
                args.Target == null)
                return false;

            if (!_whitelist.IsWhitelistPass(ent.Comp.RestrictionsIDs, args.Target.Value))
                return false;

            return true;
        }
    }
}
