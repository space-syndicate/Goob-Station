using Robust.Shared.Map;
using Content.Server.Imperial.MTFCall;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.IoC;

namespace Content.Server.Imperial.MTFCall;

public sealed class CallMTFSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;

    public bool SpawnMTF(MTFCallPresetPrototype preset)
    {
        var shuttleMapUid = _mapSystem.CreateMap();
        var mapId = Comp<MapComponent>(shuttleMapUid).MapId;

        var options = new DeserializationOptions()
        {
            InitializeMaps = true
        };
        if (_map.TryLoadGrid(mapId, preset.Path, out _, options))
        {
            return true;
        }
        Log.Error($"Не удалось загрузить сетку МОГ из пути: {preset.Path.ToString()}");
        _mapSystem.DeleteMap(mapId);
        return false;
    }
}
