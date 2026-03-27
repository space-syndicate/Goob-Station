using Content.Shared.Imperial.ImperialStore;
using Content.Shared.Whitelist;

namespace Content.Server.Imperial.ImperialStore;

/// <summary>
/// Filters out an entry based on the components or tags on an entity.
/// </summary>
public sealed partial class ImperialBuyerWhitelistCondition : ImperialListingCondition
{
    /// <summary>
    /// A whitelist of tags or components.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of tags or components.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    public override bool Condition(ImperialListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var whitelistSystem = ent.System<EntityWhitelistSystem>();

        if (whitelistSystem.IsWhitelistFail(Whitelist, args.Buyer) ||
            whitelistSystem.IsWhitelistPass(Blacklist, args.Buyer))
            return false;

        return true;
    }
}
