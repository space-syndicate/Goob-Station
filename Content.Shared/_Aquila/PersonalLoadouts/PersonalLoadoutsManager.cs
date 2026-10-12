using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Shared;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.PersonalLoadouts;

/// <summary>
/// Собирает лоадауты с <see cref="PersonalLoadoutEffect"/> и сопоставляет их с владельцами.
/// </summary>
public sealed class PersonalLoadoutsManager : ISharedLoadoutsManager
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private readonly Dictionary<NetUserId, List<string>> _loadouts = new();
    private ISawmill _sawmill = default!;
    private bool _dirty = true;

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("loadouts.personal");
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
    }

    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes)
    {
        EnsureCache();

        if (!_loadouts.TryGetValue(userId, out var cached))
        {
            prototypes = null;
            return false;
        }

        prototypes = new List<string>(cached);
        return true;
    }

    public List<string> GetClientPrototypes()
    {
        if (_player.LocalUser is { } user && TryGetServerPrototypes(user, out var prototypes))
            return prototypes;

        return new List<string>();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<LoadoutPrototype>())
            _dirty = true;
    }

    private void EnsureCache()
    {
        if (!_dirty)
            return;

        _dirty = false;
        _loadouts.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<LoadoutPrototype>())
        {
            foreach (var effect in proto.Effects)
            {
                if (effect is not PersonalLoadoutEffect personal)
                    continue;

                foreach (var user in personal.Users)
                {
                    if (!Guid.TryParse(user, out var guid))
                    {
                        _sawmill.Error($"Invalid user id '{user}' in personal loadout {proto.ID}");
                        continue;
                    }

                    var list = _loadouts.GetOrNew(new NetUserId(guid));
                    if (!list.Contains(proto.ID))
                        list.Add(proto.ID);
                }
            }
        }
    }
}
