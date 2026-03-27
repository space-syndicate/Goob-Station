using Content.Shared.Imperial.ImperialStore;
using Content.Shared.Whitelist;

namespace Content.Server.Imperial.ImperialStore;

/// <summary>
/// Filters out an entry based on the components or tags on the store itself.
/// </summary>
public sealed partial class ImperialStoreWhitelistCondition : ImperialListingCondition
{
    /// <summary>
    /// A whitelist of tags or components.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of tags or components.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;


    public override bool Condition(ImperialListingConditionArgs args)
    {
        if (args.StoreEntity == null)
            return false;

        var ent = args.EntityManager;
        var whitelistSystem = ent.System<EntityWhitelistSystem>();

        if (whitelistSystem.IsWhitelistFail(Whitelist, args.StoreEntity.Value) ||
            whitelistSystem.IsWhitelistPass(Blacklist, args.StoreEntity.Value))
            return false;

        return true;
    }
}
