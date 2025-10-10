using Content.Server.Maps;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.TileReactions
{
    [DataDefinition]
    public sealed partial class ReplaceTileReaction : ITileReaction
    {
        [DataField("tile", required: true)]
        private ProtoId<ContentTileDefinition> _replacedTile = default!;

        [DataField("minVolume")]
        private int _minVolume = 0;

        public FixedPoint2 TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume, IEntityManager entityManager, List<ReagentData>? data = null)
        {
            if (reactVolume < _minVolume) return FixedPoint2.Zero;

            var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();

            if (entityManager.EntitySysManager.TryGetEntitySystem<TileSystem>(out var tileSystem))
                tileSystem.ReplaceTile(tile, (ContentTileDefinition) tileDefManager[_replacedTile.Id]);

            return FixedPoint2.Zero;
        }
    }
}


