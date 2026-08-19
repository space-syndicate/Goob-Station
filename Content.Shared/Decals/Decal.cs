// SPDX-License-Identifier: MIT

using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Decal
    {
        // if these are made not-readonly, then decal grid state handling needs to be updated to clone decals.
        [DataField("coordinates")] public Vector2 Coordinates = Vector2.Zero;
        [DataField("id")] public  string Id = string.Empty;
        [DataField("color")] public  Color? Color;
        [DataField("angle")] public  Angle Angle = Angle.Zero;
        [DataField("zIndex")] public  int ZIndex;
        [DataField("cleanable")] public  bool Cleanable;
        // corvax-goob start
        [DataField("glows")] public  bool Glows;
        /// <summary>
        /// How long the decal should glow in seconds? For infinity set -1
        /// </summary>
        [DataField("glowDuration")] public float GlowDuration = 1200; // 20 minutes
        /// <summary>
        /// How strong should be the glow when decal was created? normalized
        /// </summary>
        [DataField("glowEnergy")] public float GlowEnergy = 0.25f;
        //corvax-goob end

        public Decal() {}

        public Decal(Vector2 coordinates, string id, Color? color, Angle angle, int zIndex, bool cleanable, bool glows, float glowDuration, float glowEnergy)
        {
            Coordinates = coordinates;
            Id = id;
            Color = color;
            Angle = angle;
            ZIndex = zIndex;
            Cleanable = cleanable;
            // corvax-goob
            Glows = glows;
            GlowDuration = glowDuration;
            GlowEnergy = glowEnergy;
        }

        public Decal WithCoordinates(Vector2 coordinates) => new(coordinates, Id, Color, Angle, ZIndex, Cleanable, Glows, GlowDuration, GlowEnergy);
        public Decal WithId(string id) => new(Coordinates, id, Color, Angle, ZIndex, Cleanable, Glows, GlowDuration, GlowEnergy);
        public Decal WithColor(Color? color) => new(Coordinates, Id, color, Angle, ZIndex, Cleanable, Glows, GlowDuration, GlowEnergy);
        public Decal WithRotation(Angle angle) => new(Coordinates, Id, Color, angle, ZIndex, Cleanable, Glows, GlowDuration, GlowEnergy);
        public Decal WithZIndex(int zIndex) => new(Coordinates, Id, Color, Angle, zIndex, Cleanable, Glows, GlowDuration, GlowEnergy);
        public Decal WithCleanable(bool cleanable) => new(Coordinates, Id, Color, Angle, ZIndex, cleanable, Glows, GlowDuration, GlowEnergy);
        public Decal WithGlows(bool glows) => new(Coordinates, Id, Color, Angle, ZIndex, Cleanable, glows, GlowDuration, GlowEnergy);
        public Decal WithGlowTime(float glowDuration) => new(Coordinates, Id, Color, Angle, ZIndex, Cleanable, Glows, glowDuration, GlowEnergy);
        public Decal WithGlowEnergy(float glowEnergy) => new(Coordinates, Id, Color, Angle, ZIndex, Cleanable, Glows, GlowDuration, glowEnergy);
    }
}
