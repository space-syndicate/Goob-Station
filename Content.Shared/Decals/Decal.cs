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
        // corvax-goob
        [DataField("glows")] public  bool Glows;

        public Decal() {}

        public Decal(Vector2 coordinates, string id, Color? color, Angle angle, int zIndex, bool cleanable, bool glows)
        {
            Coordinates = coordinates;
            Id = id;
            Color = color;
            Angle = angle;
            ZIndex = zIndex;
            Cleanable = cleanable;
            // corvax-goob
            Glows = glows;
        }

        public Decal WithCoordinates(Vector2 coordinates) => new(coordinates, Id, Color, Angle, ZIndex, Cleanable, Glows);
        public Decal WithId(string id) => new(Coordinates, id, Color, Angle, ZIndex, Cleanable, Glows);
        public Decal WithColor(Color? color) => new(Coordinates, Id, color, Angle, ZIndex, Cleanable, Glows);
        public Decal WithRotation(Angle angle) => new(Coordinates, Id, Color, angle, ZIndex, Cleanable, Glows);
        public Decal WithZIndex(int zIndex) => new(Coordinates, Id, Color, Angle, zIndex, Cleanable, Glows);
        public Decal WithCleanable(bool cleanable) => new(Coordinates, Id, Color, Angle, ZIndex, cleanable, Glows);
        public Decal WithGlows(bool glows) => new(Coordinates, Id, Color, Angle, ZIndex, Cleanable, glows);
    }
}
