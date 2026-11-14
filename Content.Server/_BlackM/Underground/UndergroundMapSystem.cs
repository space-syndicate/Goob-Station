using System.Threading.Tasks;
using Content.Server.Atmos.Components;
using Content.Server.Decals;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.GameTicking;
using Content.Server.Procedural;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Mobs.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Decals;
using Content.Shared._BlackM.Underground;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Parallax;
using Content.Shared.Procedural;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Components;

namespace Content.Server._BlackM.Underground;

public sealed record UndergroundGeneratedMap(EntityUid Map, MapId MapId, IReadOnlyList<Dungeon> Dungeons, Vector2i LandingTile, EntityUid LandingDispatcher, int Seed);

public sealed class UndergroundMapSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _transforms = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly DungeonSystem _dungeons = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private const string DungeonPrototype = "BlackMUndergroundComplex";
    private const string CorridorPalettePrototype = "BlackMUndergroundCorridorPalette";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private readonly Dictionary<EntityUid, EntityUid> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    public async Task<(UndergroundGeneratedMap? Site, bool Retryable)> GenerateAsync(EntityUid owner, int seed)
    {
        if (TerminatingOrDeleted(owner))
            return (null, false);

        if (_pending.ContainsKey(owner))
        {
            Log.Error("Underground generation is already pending for this console.");
            return (null, false);
        }

        if (!_prototypes.TryIndex<DungeonConfigPrototype>(DungeonPrototype, out var config))
        {
            Log.Error($"Underground dungeon prototype {DungeonPrototype} was not found.");
            return (null, false);
        }

        EntityUid? mapUid = null;
        var published = false;
        try
        {
            mapUid = _maps.CreateMap(out var mapId, runMapInit: false);
            _pending.Add(owner, mapUid.Value);
            var grid = EnsureComp<MapGridComponent>(mapUid.Value);
            var parallax = EnsureComp<ParallaxComponent>(mapUid.Value);
            parallax.Parallax = "BlackM";
            Dirty(mapUid.Value, parallax);
            var gravity = EnsureComp<GravityComponent>(mapUid.Value);
            gravity.Enabled = true;
            gravity.Inherent = true;
            Dirty(mapUid.Value, gravity);

            var atmosphere = EnsureComp<MapAtmosphereComponent>(mapUid.Value);
            var gas = new float[Atmospherics.AdjustedNumberOfGases];
            gas[0] = 21.824779f;
            gas[1] = 82.10312f;
            _atmosphere.SetMapSpace(mapUid.Value, false, atmosphere);
            _atmosphere.SetMapGasMixture(mapUid.Value, new GasMixture(gas, Atmospherics.T20C), atmosphere);
            _maps.InitializeMap(mapId);
            _maps.SetPaused(mapUid.Value, true);

            var generated = await _dungeons.GenerateDungeonAsync(config, mapUid.Value, grid, Vector2i.Zero, seed);
            if (TerminatingOrDeleted(owner) || TerminatingOrDeleted(mapUid.Value) ||
                !_pending.TryGetValue(owner, out var current) || current != mapUid.Value)
            {
                return (null, false);
            }

            if (generated.Count == 0)
            {
                Log.Error($"Underground generation produced no dungeons (seed {seed}); retrying if possible.");
                return (null, true);
            }

            var hasRooms = false;
            foreach (var dungeon in generated)
            {
                if (dungeon.Rooms.Count > 0)
                {
                    hasRooms = true;
                    break;
                }
            }

            if (!hasRooms)
            {
                Log.Error($"Underground generation found no rooms (seed {seed}); retrying if possible.");
                return (null, true);
            }

            RemoveComplexAirlocks(mapUid.Value);

            if (!ApplyCorridorPalette(mapUid.Value, grid, generated, seed))
                return (null, false);

            if (!TryBuildLandingCabin(mapUid.Value, grid, generated, out var landingTile, out var landingDispatcher))
            {
                Log.Error($"Underground generation could not place a connected elevator cabin (seed {seed}); retrying if possible.");
                return (null, true);
            }

            AddOuterWalls(mapUid.Value, grid, generated, landingTile);
            AddInfectionDecals(mapUid.Value, grid, generated, landingTile, seed);
            SpawnXenCrystals(mapUid.Value, grid, generated, landingTile, seed);
            _maps.SetPaused(mapUid.Value, false);
            _pending.Remove(owner);
            published = true;
            return (new UndergroundGeneratedMap(mapUid.Value, mapId, generated, landingTile, landingDispatcher, seed), false);
        }
        catch (Exception e)
        {
            Log.Error($"Underground generation failed for {ToPrettyString(owner)} (seed {seed}): {e}");
            return (null, false);
        }
        finally
        {
            if (!published && mapUid is { } map && _pending.TryGetValue(owner, out var current) && current == map)
            {
                _pending.Remove(owner);
                if (!TerminatingOrDeleted(map))
                    QueueDel(map);
            }
        }
    }

    public bool TryPlaceMissionTargets(UndergroundGeneratedMap site, EntityUid owner,
        UndergroundMission mission, long generation)
    {
        if (!TryComp<MapGridComponent>(site.Map, out var grid))
            return false;
        var candidates = new List<Vector2i>();
        var seen = new HashSet<Vector2i>();
        foreach (var dungeon in site.Dungeons)
        foreach (var tile in dungeon.RoomTiles)
        {
            if (!seen.Add(tile) || !IsUsableFloor(grid, tile) ||
                Math.Abs(tile.X - site.LandingTile.X) < 8 && Math.Abs(tile.Y - site.LandingTile.Y) < 8)
                continue;
            var coords = _maps.GridTileToLocal(site.Map, grid, tile);
            var blocked = false;
            foreach (var body in _lookup.GetEntitiesInRange<PhysicsComponent>(coords, 0.45f))
            {
                if (body.Comp.CanCollide && body.Comp.Hard)
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked)
                candidates.Add(tile);
        }
        var random = new Random(site.Seed);
        for (var i = candidates.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        if (UndergroundMissions.IsItem(mission))
        {
            if (candidates.Count == 0)
                return false;
            var item = Spawn(UndergroundMissions.ItemPrototype(mission), _maps.GridTileToLocal(site.Map, grid, candidates[0]));
            SpawnGuards(site.Map, grid, candidates[0], ObjectiveGuards, random);
            var marker = EnsureComp<UndergroundMissionSampleComponent>(item);
            marker.Console = owner;
            marker.Generation = generation;
            marker.Mission = mission;
            return true;
        }
        var existing = new List<EntityUid>();
        var mobs = EntityQueryEnumerator<MobStateComponent, MetaDataComponent, TransformComponent>();
        while (mobs.MoveNext(out var uid, out _, out var meta, out var transform))
        {
            if (transform.MapUid == site.Map && meta.EntityPrototype?.ID == UndergroundMissions.Target(mission))
                existing.Add(uid);
        }
        foreach (var uid in existing)
        {
            var marker = EnsureComp<UndergroundMissionTargetComponent>(uid);
            marker.Console = owner;
            marker.Generation = generation;
        }
        var count = UndergroundMissions.IsSample(mission) ? 2 : UndergroundMissions.Goal(mission);
        if (candidates.Count < count)
            return false;
        for (var i = 0; i < count; i++)
        {
            var target = Spawn(UndergroundMissions.Target(mission), _maps.GridTileToLocal(site.Map, grid, candidates[i]));
            if (i % 2 == 0)
                SpawnGuards(site.Map, grid, candidates[i], 1, random);
            var marker = EnsureComp<UndergroundMissionTargetComponent>(target);
            marker.Console = owner;
            marker.Generation = generation;
        }
        return true;
    }

    private void AddInfectionDecals(EntityUid map, MapGridComponent grid, IReadOnlyList<Dungeon> dungeons,
        Vector2i landing, int seed)
    {
        EnsureComp<DecalGridComponent>(map);
        var tiles = new HashSet<Vector2i>();
        foreach (var dungeon in dungeons)
        {
            tiles.UnionWith(dungeon.RoomTiles);
            tiles.UnionWith(dungeon.CorridorTiles);
        }
        var candidates = new List<Vector2i>();
        foreach (var tile in tiles)
        {
            if (Math.Abs(tile.X - landing.X) <= 4 && Math.Abs(tile.Y - landing.Y) <= 4)
                continue;
            if (IsUsableFloor(grid, tile))
                candidates.Add(tile);
        }
        var random = new Random(unchecked(seed ^ 0x5a17));
        for (var i = candidates.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        var limit = Math.Min(120, Math.Max(1, candidates.Count / 7));
        var placed = new List<Vector2i>();
        foreach (var tile in candidates)
        {
            if (placed.Count >= limit)
                break;
            var tooClose = false;
            foreach (var other in placed)
            {
                var dx = tile.X - other.X;
                var dy = tile.Y - other.Y;
                if (dx * dx + dy * dy < 4)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;
            var coords = _maps.GridTileToLocal(map, grid, tile).Offset(-grid.TileSizeHalfVector);
            var decal = random.Next(2) == 0 ? "AmsInfectedDecal1" : "AmsInfectedDecal2";
            if (_decals.TryAddDecal(decal, coords, out _, cleanable: true))
                placed.Add(tile);
        }
        if (candidates.Count > 0 && placed.Count == 0)
            Log.Error("Underground infection decals could not be placed on any candidate tile.");
    }

    private void RemoveComplexAirlocks(EntityUid map)
    {
        var doors = new List<EntityUid>();
        var query = EntityQueryEnumerator<AirlockComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            if (transform.MapUid == map)
                doors.Add(uid);
        }
        foreach (var door in doors)
            Del(door);
    }

    // Three independently mapped 3x3 floor swatches and wall samples define
    // appearance. This changes appearance only, not path topology or room contents.
    private bool ApplyCorridorPalette(EntityUid map, MapGridComponent grid, IReadOnlyList<Dungeon> dungeons, int seed)
    {
        if (!_prototypes.TryIndex<DungeonRoomPrototype>(CorridorPalettePrototype, out var palette))
        {
            Log.Error("Underground corridor palette prototype is missing.");
            return false;
        }

        var templateMap = _mapManager.GetMapEntityId(_dungeons.GetOrCreateTemplate(palette));
        if (!TryComp<MapGridComponent>(templateMap, out var templateGrid))
            return false;

        var samples = new Tile[3, 3, 3];
        var wallPrototypes = new string[3];
        for (var style = 0; style < 3; style++)
        {
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
            {
                var index = palette.Offset + new Vector2i(style * 5 + x, y);
                if (!_maps.TryGetTileDef(templateGrid, index, out var definition) ||
                    definition is not ContentTileDefinition floor || floor.ID == ContentTileDefinition.SpaceID ||
                    floor.MapAtmosphere)
                {
                    Log.Error($"Underground corridor palette has an unsafe floor at {index}.");
                    return false;
                }
                samples[style, x, y] = _maps.GetTileRef(templateMap, templateGrid, index).Tile;
            }

            var wallIndex = palette.Offset + new Vector2i(style * 5 + 4, 1);
            var sampleWalls = _maps.GetAnchoredEntitiesEnumerator(templateMap, templateGrid, wallIndex);
            string? wallPrototype = null;
            while (sampleWalls.MoveNext(out var sample))
            {
                var uid = sample.Value;
                if (!_tags.HasTag(uid, WallTag) || !TryComp<MetaDataComponent>(uid, out var meta) ||
                    meta.EntityPrototype?.ID is not { } id)
                    continue;
                if (wallPrototype != null)
                {
                    Log.Error($"Underground corridor palette has multiple wall samples at {wallIndex}.");
                    return false;
                }
                wallPrototype = id;
            }
            if (wallPrototype == null)
            {
                Log.Error($"Underground corridor palette has no wall sample at {wallIndex}.");
                return false;
            }
            wallPrototypes[style] = wallPrototype;
        }

        var tiles = new List<(Vector2i, Tile)>();
        var replaceWalls = new Dictionary<EntityUid, (Vector2i Tile, string Prototype)>();
        foreach (var dungeon in dungeons)
        {
            foreach (var tile in dungeon.CorridorTiles)
            {
                if (dungeon.RoomTiles.Contains(tile) || dungeon.Entrances.Contains(tile))
                    continue;
                var x = (tile.X % 3 + 3) % 3;
                var y = (tile.Y % 3 + 3) % 3;
                tiles.Add((tile, samples[CorridorStyle(tile, seed), x, y]));
            }

            foreach (var tile in dungeon.CorridorExteriorTiles)
            {
                if (dungeon.RoomTiles.Contains(tile) || dungeon.Entrances.Contains(tile))
                    continue;
                var replacement = wallPrototypes[CorridorStyle(tile, seed)];
                if (replacement == "WallSolid")
                    continue;
                var entities = _maps.GetAnchoredEntitiesEnumerator(map, grid, tile);
                while (entities.MoveNext(out var entity))
                {
                    var uid = entity.Value;
                    if (TryComp<MetaDataComponent>(uid, out var meta) && meta.EntityPrototype?.ID == "WallSolid")
                        replaceWalls.TryAdd(uid, (tile, replacement));
                }
            }
        }

        _maps.SetTiles(map, grid, tiles);
        foreach (var (wall, value) in replaceWalls)
        {
            Del(wall);
            Spawn(value.Prototype, _maps.GridTileToLocal(map, grid, value.Tile));
        }
        return true;
    }

    private static int CorridorStyle(Vector2i tile, int seed)
    {
        // Stable 9x9 regions produce varied runs instead of flickering each tile.
        var x = (int) Math.Floor(tile.X / 9.0);
        var y = (int) Math.Floor(tile.Y / 9.0);
        var hash = unchecked((uint) x * 73856093u ^ (uint) y * 19349663u ^ (uint) seed);
        return (int) (hash % 3u);
    }

    // Cover the generated perimeter, including windows and untagged wall types.
    // Never overwrite a generated tile, floor, or obstruction.
    private void AddOuterWalls(EntityUid map, MapGridComponent grid, IReadOnlyList<Dungeon> dungeons,
        Vector2i landing)
    {
        var occupied = new HashSet<Vector2i>();
        var boundary = new HashSet<Vector2i>();
        foreach (var dungeon in dungeons)
        {
            occupied.UnionWith(dungeon.AllTiles);
            boundary.UnionWith(dungeon.RoomExteriorTiles);
            boundary.UnionWith(dungeon.CorridorExteriorTiles);
        }

        // Include the cabin perimeter as it is built after dungeon generation.
        for (var x = -2; x <= 2; x++)
        for (var y = -2; y <= 2; y++)
        {
            if (Math.Abs(x) == 2 || Math.Abs(y) == 2)
                boundary.Add(landing + new Vector2i(x, y));
        }

        var candidates = new HashSet<Vector2i>();
        foreach (var edge in boundary)
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            if (x == 0 && y == 0)
                continue;
            var candidate = edge + new Vector2i(x, y);
            if (candidates.Contains(candidate) || !IsEmptyOutsideTile(map, grid, occupied, candidate))
                continue;
            candidates.Add(candidate);
        }

        var floor = new Tile(_tiles["FloorSteel"].TileId);
        foreach (var tile in candidates)
        {
            _maps.SetTile(map, grid, tile, floor);
            Spawn("WallBmesaIndestructible", _maps.GridTileToLocal(map, grid, tile));
        }
    }

    // Attach a small physical elevator room to a free corridor wall. The existing
    // ride still happens in the isolated elevator map; passengers arrive here.
    private bool TryBuildLandingCabin(EntityUid map, MapGridComponent grid, IReadOnlyList<Dungeon> dungeons,
        out Vector2i landing, out EntityUid dispatcher)
    {
        var occupied = new HashSet<Vector2i>();
        foreach (var dungeon in dungeons)
            occupied.UnionWith(dungeon.AllTiles);

        var directions = new[] { new Vector2i(0, 1), new Vector2i(1, 0),
            new Vector2i(0, -1), new Vector2i(-1, 0) };
        foreach (var dungeon in dungeons)
        foreach (var boundary in dungeon.CorridorExteriorTiles)
        foreach (var direction in directions)
        {
            var inside = boundary - direction;
            if (!dungeon.CorridorTiles.Contains(inside) || !IsUsableFloor(grid, inside))
                continue;

            var walls = new List<EntityUid>();
            var anchors = _maps.GetAnchoredEntitiesEnumerator(map, grid, boundary);
            var obstructed = false;
            while (anchors.MoveNext(out var entity))
            {
                var uid = entity.Value;
                if (_tags.HasTag(uid, WallTag))
                    walls.Add(uid);
                else if (TryComp<PhysicsComponent>(uid, out var body) && body.CanCollide && body.Hard)
                    obstructed = true;
            }
            if (obstructed || walls.Count == 0)
                continue;

            var center = boundary + direction * 4;
            if (!IsEmptyOutsideTile(map, grid, occupied, boundary + direction))
                continue;
            var free = true;
            for (var x = -2; x <= 2 && free; x++)
            for (var y = -2; y <= 2; y++)
            {
                if (IsEmptyOutsideTile(map, grid, occupied, center + new Vector2i(x, y)))
                    continue;
                free = false;
                break;
            }
            if (!free)
                continue;

            // No player or cargo is on this map yet. Abort the whole unpublished
            // map on an exception instead of publishing a partially built cabin.
            var floor = new Tile(_tiles["FloorReinforced"].TileId);
            _maps.SetTile(map, grid, boundary + direction, floor);
            for (var x = -2; x <= 2; x++)
            for (var y = -2; y <= 2; y++)
                _maps.SetTile(map, grid, center + new Vector2i(x, y), floor);

            var opening = center - direction * 2;
            for (var x = -2; x <= 2; x++)
            for (var y = -2; y <= 2; y++)
            {
                if (Math.Abs(x) != 2 && Math.Abs(y) != 2)
                    continue;
                var tile = center + new Vector2i(x, y);
                if (tile == opening)
                    continue;
                Spawn("WallSolid", _maps.GridTileToLocal(map, grid, tile));
            }

            foreach (var wall in walls)
                QueueDel(wall);
            Spawn("BlackMUndergroundCabinDoor", _maps.GridTileToLocal(map, grid, opening));
            // Match the travelling cabin: mount on its left wall, rotated with the exit.
            var dispatcherTile = center + new Vector2i(-2 * direction.Y, 2 * direction.X);
            dispatcher = Spawn("BlackMElevatorDispatcher", _maps.GridTileToLocal(map, grid, dispatcherTile));
            var rotation = -Math.Atan2(direction.X, direction.Y) * 180.0 / Math.PI;
            _transforms.SetLocalRotation(dispatcher, Angle.FromDegrees(90 + rotation));
            AddCabinDetails(map, grid, center, direction);
            Spawn("BlackMElevatorLight", _maps.GridTileToLocal(map, grid, center));
            landing = center;
            return true;
        }

        landing = default;
        dispatcher = default;
        return false;
    }

    private void AddCabinDetails(EntityUid map, MapGridComponent grid, Vector2i center, Vector2i direction)
    {
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
            Spawn("Catwalk", _maps.GridTileToLocal(map, grid, center + new Vector2i(x, y)));

        var rotation = -Math.Atan2(direction.X, direction.Y) * 180.0 / Math.PI;
        AddRail("Railing", 1, 0, 90);
        AddRail("Railing", -1, 0, -90);
        AddRail("RailingCornerSmall", -1, -1, 0);
        AddRail("RailingCornerSmall", -1, 1, 90);
        AddRail("RailingCornerSmall", 1, 1, 180);
        AddRail("RailingCornerSmall", 1, -1, -90);

        void AddRail(string prototype, int x, int y, int baseAngle)
        {
            var offset = new Vector2i(direction.Y * x + direction.X * y,
                -direction.X * x + direction.Y * y);
            var rail = Spawn(prototype, _maps.GridTileToLocal(map, grid, center + offset));
            _transforms.SetLocalRotation(rail, Angle.FromDegrees(baseAngle + rotation));
        }
    }

    private bool IsEmptyOutsideTile(EntityUid map, MapGridComponent grid, HashSet<Vector2i> occupied, Vector2i tile)
    {
        if (occupied.Contains(tile) || !_anchorable.TileFree(grid, tile,
                DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
            return false;
        if (_maps.TryGetTileDef(grid, tile, out var tileDef) && tileDef.ID != ContentTileDefinition.SpaceID)
            return false;
        var coords = _maps.GridTileToLocal(map, grid, tile);
        foreach (var body in _lookup.GetEntitiesInRange<PhysicsComponent>(coords, 0.45f))
        {
            if (body.Comp.CanCollide && body.Comp.Hard)
                return false;
        }
        return true;
    }

    private const string XenCrystalPrototype = "MaterialXenCrystalConcentrate1";
    private const int XenCrystalMin = 1;
    private const int XenCrystalMax = 2;
    private const int XenCrystalMinDistance = 14;
    private const int CrystalGuards = 2;
    private const int ObjectiveGuards = 2;
    private static readonly string[] GuardPool =
    {
        "MobHeadcrabBlackM",
        "MobHeadcrabBlackM",
        "MobHoundeyeBlackM",
        "MobHoundeyeBlackM",
        "MobVortigauntBlackM",
    };

    private void SpawnXenCrystals(EntityUid map, MapGridComponent grid, IReadOnlyList<Dungeon> dungeons,
        Vector2i landing, int seed)
    {
        var random = new Random(seed ^ 0x5EED);
        var candidates = new List<Vector2i>();
        var seen = new HashSet<Vector2i>();
        foreach (var dungeon in dungeons)
        foreach (var tile in dungeon.RoomTiles)
        {
            if (!seen.Add(tile) || !IsUsableFloor(grid, tile))
                continue;
            if (Math.Abs(tile.X - landing.X) + Math.Abs(tile.Y - landing.Y) < XenCrystalMinDistance)
                continue;
            candidates.Add(tile);
        }

        if (candidates.Count == 0)
        {
            foreach (var dungeon in dungeons)
            foreach (var tile in dungeon.RoomTiles)
            {
                if (IsUsableFloor(grid, tile) && tile != landing)
                    candidates.Add(tile);
            }
        }

        if (candidates.Count == 0)
        {
            Log.Warning($"Underground: no free tile for Xen crystals (seed {seed}).");
            return;
        }

        var count = random.Next(XenCrystalMin, XenCrystalMax + 1);
        var placed = new List<Vector2i>();
        for (var attempt = 0; attempt < 50 && placed.Count < count; attempt++)
        {
            var tile = candidates[random.Next(candidates.Count)];
            var tooClose = false;
            foreach (var other in placed)
            {
                if (Math.Abs(other.X - tile.X) + Math.Abs(other.Y - tile.Y) < 10)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose && attempt < 40)
                continue;

            placed.Add(tile);
            Spawn(XenCrystalPrototype, _maps.GridTileToLocal(map, grid, tile));
            SpawnGuards(map, grid, tile, CrystalGuards, random);
        }
    }

    private void SpawnGuards(EntityUid map, MapGridComponent grid, Vector2i center, int count, Random random)
    {
        var spots = new List<Vector2i>();
        for (var x = -3; x <= 3; x++)
        for (var y = -3; y <= 3; y++)
        {
            if (Math.Abs(x) + Math.Abs(y) < 2)
                continue;
            var tile = center + new Vector2i(x, y);
            if (IsUsableFloor(grid, tile))
                spots.Add(tile);
        }

        for (var i = 0; i < count && spots.Count > 0; i++)
        {
            var index = random.Next(spots.Count);
            var tile = spots[index];
            spots.RemoveAt(index);
            Spawn(GuardPool[random.Next(GuardPool.Length)], _maps.GridTileToLocal(map, grid, tile));
        }
    }

    private bool IsUsableFloor(MapGridComponent grid, Vector2i tile)
    {
        return _maps.TryGetTileDef(grid, tile, out var tileDef) &&
               tileDef is ContentTileDefinition floor &&
               floor.ID != ContentTileDefinition.SpaceID && !floor.MapAtmosphere &&
               _anchorable.TileFree(grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent args)
    {
        foreach (var map in _pending.Values)
        {
            if (!TerminatingOrDeleted(map))
                QueueDel(map);
        }

        _pending.Clear();
    }
}
