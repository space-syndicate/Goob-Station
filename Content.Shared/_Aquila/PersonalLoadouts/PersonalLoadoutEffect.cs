using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.PersonalLoadouts;

/// <summary>
/// Делает лоадаут доступным только игрокам с указанными UUID.
/// </summary>
public sealed partial class PersonalLoadoutEffect : LoadoutEffect
{
    /// <summary>
    /// UUID игроков, которым принадлежит этот лоадаут.
    /// </summary>
    [DataField(required: true)]
    public List<string> Users = new();

    public bool IsOwner(NetUserId userId)
    {
        foreach (var user in Users)
        {
            if (Guid.TryParse(user, out var guid) && guid == userId.UserId)
                return true;
        }

        return false;
    }

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        LoadoutPrototype proto,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (session != null && IsOwner(session.UserId))
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-personal-only"));
        return false;
    }
}
