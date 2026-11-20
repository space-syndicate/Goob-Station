using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._BlackM.Explosion;

public sealed class BMExplosionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly Color DebrisColor = new(0.32f, 0.3f, 0.28f, 1f);
    private const string Path = "/Textures/_BlackM/Effects/Explosion/";

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly BMExplosionSystem _system;
    private readonly ShaderInstance _unshaded;
    private readonly Texture[] _fire;
    private readonly Texture[] _smoke;
    private readonly Texture[] _debris;
    private readonly Texture _glow;
    private readonly Texture _spark;

    public BMExplosionOverlay(BMExplosionSystem system, IResourceCache cache, IPrototypeManager proto)
    {
        _system = system;
        _unshaded = proto.Index(UnshadedShader).Instance();

        var fire = cache.GetResource<RSIResource>(Path + "fire.rsi").RSI;
        var smoke = cache.GetResource<RSIResource>(Path + "smoke.rsi").RSI;
        var fx = cache.GetResource<RSIResource>(Path + "fx.rsi").RSI;

        _fire = Frames(fire, "puff", 4);
        _smoke = Frames(smoke, "smoke", 4);
        _debris = Frames(fx, "debris", 3);
        _glow = fx["glow"].GetFrames(default)[0];
        _spark = fx["spark"].GetFrames(default)[0];
    }

    private static Texture[] Frames(RSI rsi, string prefix, int count)
    {
        var result = new Texture[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = rsi[$"{prefix}{i + 1}"].GetFrames(default)[0];
        }

        return result;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var effects = _system.Effects;
        if (effects.Count == 0)
            return;

        var handle = args.WorldHandle;
        var view = args.WorldAABB.Enlarged(3f);
        var now = _system.Now;

        handle.UseShader(null);
        foreach (var fx in effects)
        {
            if (fx.Map == args.MapId)
                DrawLit(handle, fx, view);
        }

        handle.UseShader(_unshaded);
        foreach (var fx in effects)
        {
            if (fx.Map != args.MapId)
                continue;

            DrawFire(handle, fx, view, now);
            DrawSparks(handle, fx, view);
            DrawBlast(handle, fx, view, now);
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }

    private void DrawLit(DrawingHandleWorld handle, BMExplosionEffect fx, Box2 view)
    {
        var smokeStart = Color.InterpolateBetween(fx.Late, fx.Smoke, 0.35f);

        foreach (var p in fx.Particles)
        {
            if (p.Age < 0f || p.Kind is BMParticleKind.Spark or BMParticleKind.Ember)
                continue;

            if (!TryWorld(fx, p.Group, p.Pos, out var pos) || !view.Contains(pos))
                continue;

            var t = p.Age / p.Life;

            if (p.Kind == BMParticleKind.Smoke)
            {
                var size = p.Size + (p.EndSize - p.Size) * EaseOut(t);
                var alpha = p.Alpha * MathF.Min(t / 0.12f, 1f) * (1f - SmoothStep(0.45f, 1f, t));
                var color = Color.InterpolateBetween(smokeStart, fx.Smoke, MathF.Min(t / 0.25f, 1f));
                DrawTex(handle, _smoke[p.Variant % _smoke.Length], pos, p.Rot, new Vector2(size), color.WithAlpha(alpha));
                continue;
            }

            var debrisAlpha = 1f - SmoothStep(0.7f, 1f, t);
            DrawTex(handle, _debris[p.Variant % _debris.Length], pos, p.Rot, new Vector2(p.Size), DebrisColor.WithAlpha(debrisAlpha));
        }
    }

    private void DrawFire(DrawingHandleWorld handle, BMExplosionEffect fx, Box2 view, float now)
    {
        foreach (var tile in fx.Tiles)
        {
            var age = now - tile.Ignite;
            if (age < 0f || age >= tile.Life)
                continue;

            if (!TryWorld(fx, tile.Group, tile.Local, out var pos) || !view.Contains(pos))
                continue;

            var t = age / tile.Life;
            var size = fx.Groups[tile.Group].TileSize
                       * (0.7f + 0.8f * EaseOut(MathF.Min(t / 0.25f, 1f)))
                       * (0.9f + 0.5f * tile.Heat);

            var color = t < 0.3f
                ? Color.InterpolateBetween(fx.Hot, fx.Mid, t / 0.3f)
                : Color.InterpolateBetween(fx.Mid, fx.Late, (t - 0.3f) / 0.7f);

            var alpha = 1f - SmoothStep(0.5f, 1f, t);
            var rot = tile.Seed + tile.Spin * age;

            if (fx.Lod)
            {
                DrawTex(handle, _fire[tile.Variant], pos, rot, new Vector2(size), color.WithAlpha(alpha));
                continue;
            }

            var phase = t * 3f;
            var step = (int) phase;
            var f = phase - step;
            DrawTex(handle, _fire[(tile.Variant + step) % 4], pos, rot, new Vector2(size), color.WithAlpha(alpha * (1f - f)));
            DrawTex(handle, _fire[(tile.Variant + step + 1) % 4], pos, rot + 1.3f, new Vector2(size), color.WithAlpha(alpha * f));

            var core = 1f - t / 0.45f;
            if (core > 0f)
                DrawTex(handle, _fire[(tile.Variant + 2) % 4], pos, -rot, new Vector2(size * 0.55f), fx.Hot.WithAlpha(core));
        }
    }

    private void DrawSparks(DrawingHandleWorld handle, BMExplosionEffect fx, Box2 view)
    {
        foreach (var p in fx.Particles)
        {
            if (p.Age < 0f || p.Kind is BMParticleKind.Smoke or BMParticleKind.Debris)
                continue;

            if (!TryWorld(fx, p.Group, p.Pos, out var pos) || !view.Contains(pos))
                continue;

            var t = p.Age / p.Life;

            if (p.Kind == BMParticleKind.Spark)
            {
                var speed = p.Vel.Length();
                var angle = MathF.Atan2(p.Vel.Y, p.Vel.X);
                var color = Color.InterpolateBetween(fx.Hot, fx.Mid, t).WithAlpha(1f - t);
                DrawTex(handle, _spark, pos, angle, new Vector2(speed * 0.05f + 0.1f, 0.07f), color);
                continue;
            }

            var flicker = 0.6f + 0.4f * MathF.Sin(p.Age * 25f + p.Seed);
            var ember = Color.InterpolateBetween(fx.Mid, fx.Late, t).WithAlpha((1f - t) * flicker);
            DrawTex(handle, _glow, pos, 0f, new Vector2(p.Size * 2f), ember);
        }
    }

    private void DrawBlast(DrawingHandleWorld handle, BMExplosionEffect fx, Box2 view, float now)
    {
        var r = fx.Iterations;
        if (!view.Enlarged(r + 2f).Contains(fx.Epicenter))
            return;

        var age = now - fx.Start;

        if (age < 0.22f)
        {
            var a = 1f - age / 0.22f;
            DrawTex(handle, _glow, fx.Epicenter, 0f, new Vector2(r * 1.2f + 3f), fx.Hot.WithAlpha(a * a));
        }

        if (age < 0.5f)
            DrawTex(handle, _glow, fx.Epicenter, 0f, new Vector2(r * 2f + 4f), fx.Mid.WithAlpha(0.35f * (1f - age / 0.5f)));
    }

    private static bool TryWorld(BMExplosionEffect fx, int group, Vector2 local, out Vector2 world)
    {
        if (group < 0)
        {
            world = local;
            return true;
        }

        var g = fx.Groups[group];
        world = Vector2.Transform(local, g.Matrix);
        return g.Valid;
    }

    private static float EaseOut(float x)
    {
        var inv = 1f - Math.Clamp(x, 0f, 1f);
        return 1f - inv * inv * inv;
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static void DrawTex(DrawingHandleWorld handle, Texture tex, Vector2 center, float rot, Vector2 size, Color color)
    {
        if (color.A <= 0.01f)
            return;

        handle.SetTransform(Matrix3Helpers.CreateTransform(center, rot));
        handle.DrawTextureRect(tex, Box2.CenteredAround(Vector2.Zero, size), color);
    }
}
