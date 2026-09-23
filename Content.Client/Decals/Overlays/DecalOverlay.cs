// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Decals.Overlays
{
    public sealed class DecalOverlay : GridOverlay
    {
        // CorvaxGoob-GlowDecals
        private static readonly ProtoId<ShaderPrototype> EmissiveShader = "Emissive";

        private readonly SpriteSystem _sprites;
        private readonly IEntityManager _entManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IGameTiming _timing = default!;  // CorvaxGoob-GlowDecals

        private readonly Dictionary<string, (Texture Texture, bool SnapCardinals)> _cachedTextures = new(64);

        private readonly List<(uint Id, Decal Decal)> _decals = new();

        // CorvaxGoob-Start
        private readonly ShaderInstance _emissiveShader;
        private readonly Dictionary<(EntityUid Grid, uint Id), ShaderInstance> _glowingDecalsShaders = new();
        private readonly HashSet<uint> _decalsIDs = new();
        // CorvaxGoob-End

        public DecalOverlay(
            SpriteSystem sprites,
            IEntityManager entManager,
            IPrototypeManager prototypeManager)
        {
            _sprites = sprites;
            _entManager = entManager;
            _prototypeManager = prototypeManager;
            // CorvaxGoob-GlowDecals
            _emissiveShader = _prototypeManager.Index(EmissiveShader).InstanceUnique();
            _timing = IoCManager.Resolve<IGameTiming>();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (args.MapId == MapId.Nullspace)
                return;

            var owner = Grid.Owner;

            if (!_entManager.TryGetComponent(owner, out DecalGridComponent? decalGrid) ||
                !_entManager.TryGetComponent(owner, out TransformComponent? xform))
            {
                return;
            }

            if (xform.MapID != args.MapId)
                return;

            // Shouldn't need to clear cached textures unless the prototypes get reloaded.
            var handle = args.WorldHandle;
            var xformSystem = _entManager.System<TransformSystem>();
            var eyeAngle = args.Viewport.Eye?.Rotation ?? Angle.Zero;

            var gridAABB = xformSystem.GetInvWorldMatrix(xform).TransformBox(args.WorldBounds.Enlarged(1f));
            var chunkEnumerator = new ChunkIndicesEnumerator(gridAABB, SharedDecalSystem.ChunkSize);
            _decals.Clear();
            _decalsIDs.Clear(); // CorvaxGoob-GlowDecals

            while (chunkEnumerator.MoveNext(out var index))
            {
                if (!decalGrid.ChunkCollection.ChunkCollection.TryGetValue(index.Value, out var chunk))
                    continue;

                foreach (var (id, decal) in chunk.Decals)
                {
                    if (!gridAABB.Contains(decal.Coordinates))
                        continue;

                    _decals.Add((id, decal));
                    _decalsIDs.Add(id); // CorvaxGoob-GlowDecals
                }
            }

            // CorvaxGoob-Start
            foreach (var key in _glowingDecalsShaders.Keys)
            {
                if (key.Grid == owner && !_decalsIDs.Contains(key.Id))
                    _glowingDecalsShaders.Remove(key);
            }
            // CorvaxGoob-End

            if (_decals.Count == 0)
                return;

            _decals.Sort((x, y) =>
            {
                var zComp = x.Decal.ZIndex.CompareTo(y.Decal.ZIndex);

                if (zComp != 0)
                    return zComp;

                return x.Id.CompareTo(y.Id);
            });

            var (_, worldRot, worldMatrix) = xformSystem.GetWorldPositionRotationMatrix(xform);
            handle.SetTransform(worldMatrix);

            // CorvaxGoob-GlowDecals
            var defShader = handle.GetShader();

            foreach (var (decalId, decal) in _decals)
            {
                if (!_cachedTextures.TryGetValue(decal.Id, out var cache))
                {
                    // Nothing to cache someone messed up
                    if (!_prototypeManager.TryIndex<DecalPrototype>(decal.Id, out var decalProto))
                    {
                        continue;
                    }

                    cache = (_sprites.Frame0(decalProto.Sprite), decalProto.SnapCardinals);
                    _cachedTextures[decal.Id] = cache;
                }

                var cardinal = Angle.Zero;

                if (cache.SnapCardinals)
                {
                    var worldAngle = eyeAngle + worldRot;
                    cardinal = worldAngle.GetCardinalDir().ToAngle();
                }

                var angle = decal.Angle - cardinal;

                // CorvaxGoob-Start
                if (decal.Glows)
                {
                    var drawGlow = true;
                    float glowEnergy;

                    if (decal.GlowUntil == TimeSpan.Zero)
                    {
                        glowEnergy = decal.GlowEnergy;
                    }
                    else
                    {
                        var remaining = (decal.GlowUntil - _timing.CurTime).TotalSeconds;
                        if (remaining <= 0.01)
                        {
                            _glowingDecalsShaders.Remove((owner, decalId));
                            drawGlow = false;
                            glowEnergy = 0f;
                        }
                        else
                        {
                            glowEnergy = Math.Clamp(
                                (float)(remaining / decal.GlowTime) * decal.GlowEnergy, 0f, decal.GlowEnergy);
                        }
                    }

                    if (drawGlow)
                    {
                        if (!_glowingDecalsShaders.TryGetValue((owner, decalId), out var decalShader))
                        {
                            decalShader = _emissiveShader.Duplicate();
                            _glowingDecalsShaders[(owner, decalId)] = decalShader;
                        }
                        handle.UseShader(decalShader);
                        decalShader.SetParameter("glowEnergy", glowEnergy);
                    }
                }
                // Corvax-Goob-End

                if (angle.Equals(Angle.Zero))
                    handle.DrawTexture(cache.Texture, decal.Coordinates, decal.Color);
                else
                    handle.DrawTexture(cache.Texture, decal.Coordinates, angle, decal.Color);

                handle.UseShader(defShader);
            }

            handle.SetTransform(Matrix3x2.Identity);
        }
    }
}
