using System.Numerics;

namespace Content.Shared.Imperial.ColorHelper;


public sealed class ColorHelper
{
    public static Vector3 ToVector3(Color color) => new Vector3(color.R, color.G, color.B);

    public static Vector4 ToVector4(Color color) => new Vector4(color.R, color.G, color.B, color.A);


    public static float[] ToFloat3(Color color) => new float[] { color.R, color.G, color.B };
    public static float[] ToFloat4(Color color) => new float[] { color.R, color.G, color.B, color.A };


    public static float ColorNormalized(float val) => Normalize(val, 0, 255);
    public static float Normalize(float val, float min, float max) => Math.Clamp((val - min) / (max - min), 0.0f, 1.0f);
}
