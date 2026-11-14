using System.Linq;
using System.Numerics;
using Content.Shared._BlackM.Shot;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Shot;

public sealed class ShotOverlay : Overlay
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string Path = "/Textures/_BlackM/Effects/Shot/";
    private const int MaxParticles = 500;
    private const int MaxTrailPoints = 2500;
    private const int TracerSteps = 7;
    private static readonly float Px = 1f / EyeManager.PixelsPerMeter;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly Texture[][] _puffs;
    private readonly Texture[] _wisps;
    private readonly Texture[][] _impacts;

    private readonly Dictionary<EntityUid, ShotState> _states;
    private readonly IShotWorld _world;
    private readonly List<Particle> _particles = new();

    private readonly Dictionary<EntityUid, Ribbon> _open = new();
    private readonly List<Ribbon> _ribbons = new();
    private int _trailPoints;

    private enum Kind { Puff, Impact, Spark }

    private sealed class Particle
    {
        public Kind Kind;
        public MapId Map;
        public Vector2 Pos, Vel;
        public float Z, VZ;
        public bool Falls;
        public int Bounces;
        public float Rise;
        public float Age, Life;
        public float Size;
        public float Rot, Spin;
        public float Drag;
        public float Alpha = 1f;
        public Texture[]? Frames;
        public Color Color;
        public Color? Lit;
        public float LitFrac = 0.2f;
        public float Vis = -1f;
        public int TileX = int.MinValue, TileY = int.MinValue;
    }

    private sealed class TrailPoint
    {
        public Vector2 Pos;
        public Vector2 Perp;
        public float Dist;
        public float Age;
        public float Seed;
        public int Variant;
        public float Vis = -1f;
    }

    private sealed class Ribbon
    {
        public MapId Map;
        public readonly List<TrailPoint> Points = new();
        public float Phase, Wavelength, Omega, Amp, Life, Width, Alpha, Rise;
        public Color Color;
        public Color Warm;
        public float HeadDist;
        public bool Open = true;
    }

    public ShotOverlay(IResourceCache cache, Dictionary<EntityUid, ShotState> states, IShotWorld world)
    {
        IoCManager.InjectDependencies(this);
        _states = states;
        _world = world;

        var smoke = cache.GetResource<RSIResource>(Path + "smoke.rsi").RSI;
        _puffs = Enumerable.Range(1, 4).Select(i => Frames(smoke, $"puff{i}")).ToArray();

        var trail = cache.GetResource<RSIResource>(Path + "trail.rsi").RSI;
        _wisps = Enumerable.Range(1, 6).Select(i => Frames(trail, $"wisp{i}")[0]).ToArray();

        var impact = cache.GetResource<RSIResource>(Path + "impact.rsi").RSI;
        _impacts = Enumerable.Range(1, 2).Select(i => Frames(impact, $"impact_fire{i}")).ToArray();
    }

    private static Texture[] Frames(RSI rsi, string state)
        => rsi[state].GetFrames(default);

    public void BeginTrail(EntityUid uid, MapId map, ShotComponent c)
    {
        if (!_random.Prob(c.TrailChance))
            return;

        var r = new Ribbon
        {
            Map = map,
            Phase = _random.NextFloat(6.28f),
            Wavelength = _random.NextFloat(1.0f, 1.7f),
            Omega = _random.NextFloat(2.5f, 4.5f) * (_random.Prob(0.5f) ? 1f : -1f),
            Amp = 0.14f * c.TrailWave,
            Life = c.TrailLife * _random.NextFloat(0.85f, 1.15f),
            Width = c.TrailWidth,
            Alpha = c.SmokeAlpha,
            Rise = 0.22f,
            Color = c.SmokeColor,
            Warm = Color.InterpolateBetween(c.TracerColor, Color.White, 0.5f),
        };

        _open[uid] = r;
        _ribbons.Add(r);
    }

    public void AddTrailPoint(EntityUid uid, Vector2 pos, Vector2 dir, float dist)
    {
        if (!_open.TryGetValue(uid, out var r) || _trailPoints >= MaxTrailPoints)
            return;

        r.Points.Add(new TrailPoint
        {
            Pos = pos,
            Perp = new Vector2(-dir.Y, dir.X),
            Dist = dist,
            Seed = _random.NextFloat(-1f, 1f),
            Variant = _random.Next(_wisps.Length),
        });

        r.HeadDist = dist;
    }

    public void EndTrail(EntityUid uid)
    {
        if (_open.Remove(uid, out var r))
            r.Open = false;
    }

    public void ClearTrails()
    {
        _open.Clear();
        _ribbons.Clear();
        _trailPoints = 0;
    }

    public void ClearParticles()
    {
        _particles.Clear();
    }

    public void Clear()
    {
        ClearTrails();
        ClearParticles();
    }

    private static Vector2 Displace(Ribbon r, TrailPoint p)
    {
        var grow = 0.15f + 1.3f * MathF.Sqrt(Math.Clamp(p.Age / r.Life, 0f, 1f));
        var wave = MathF.Sin(p.Dist * MathF.Tau / r.Wavelength + r.Phase + p.Age * r.Omega) * r.Amp * grow;
        wave += MathF.Sin(p.Dist * MathF.Tau / (r.Wavelength * 0.62f) + r.Phase * 1.7f - p.Age * r.Omega * 1.3f)
                * r.Amp * grow * 0.18f;
        var drift = p.Seed * 0.05f * p.Age;
        return p.Pos + p.Perp * (wave + drift) + new Vector2(0f, p.Age * r.Rise * 0.55f);
    }

    public void Impact(MapId map, Vector2 pos, Vector2 dir, ShotComponent c, bool smoke = true)
    {
        var s = c.ImpactScale;
        var back = new Vector2(-dir.X, -dir.Y);
        var backAngle = MathF.Atan2(back.Y, back.X);

        Add(new Particle
        {
            Kind = Kind.Impact,
            Map = map,
            Pos = pos,
            Rot = _random.NextFloat(6.28f),
            Life = 0.085f,
            Size = 1.0f * s,
            Frames = _random.Pick(_impacts),
            Color = Color.White,
        });

        for (var i = 0; i < (int) MathF.Round(8 * s); i++)
        {
            var a = backAngle + _random.NextFloat(-1.2f, 1.2f);
            Add(new Particle
            {
                Kind = Kind.Spark,
                Map = map,
                Pos = pos,
                Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * _random.NextFloat(2.2f, 6.5f),
                Z = _random.NextFloat(0.05f, 0.2f),
                VZ = _random.NextFloat(1f, 3.2f),
                Falls = true,
                Bounces = _random.Prob(0.4f) ? 1 : 0,
                Drag = 2.4f,
                Life = _random.NextFloat(0.3f, 0.65f),
                Size = _random.NextFloat(0.2f, 0.45f),
                Color = Color.InterpolateBetween(c.TracerColor, c.CoreColor, _random.NextFloat(0f, 0.7f)),
            });
        }

        if (!c.Smoke || !smoke)
            return;

        var sz = c.SmokeScale * s;
        for (int i = 0, n = Count(2, c.SmokeAmount); i < n; i++)
        {
            Add(new Particle
            {
                Kind = Kind.Puff,
                Map = map,
                Pos = pos + _random.NextAngle().ToVec() * 0.1f,
                Vel = back * _random.NextFloat(0.4f, 1.6f) + _random.NextAngle().ToVec() * 0.5f,
                Drag = 2.4f,
                Life = _random.NextFloat(0.8f, 1.3f),
                Size = _random.NextFloat(0.45f, 0.8f) * sz,
                Rot = _random.NextFloat(6.28f),
                Spin = _random.NextFloat(-0.9f, 0.9f),
                Frames = _random.Pick(_puffs),
                Alpha = 0.65f * c.SmokeAlpha,
                Color = c.SmokeColor,
                Lit = c.TracerColor,
                LitFrac = 0.22f,
                Rise = _random.NextFloat(0.3f, 0.5f),
            });
        }
    }

    private int Count(float baseCount, float amount)
        => Math.Max(0, (int) MathF.Round(baseCount * amount + _random.NextFloat(-0.4f, 0.4f)));

    private void Add(Particle p)
    {
        if (_particles.Count < MaxParticles)
            _particles.Add(p);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var dt = args.DeltaSeconds;
        UpdateRibbons(dt);

        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Age += dt;

            if (p.Age >= p.Life)
            {
                _particles.RemoveAt(i);
                continue;
            }

            var target = _world.Visible(p.Map, p.Pos) ? 1f : 0f;
            p.Vis = p.Vis < 0f ? target : p.Vis + Math.Clamp(target - p.Vis, -dt * 7f, dt * 7f);

            p.Rot += p.Spin * dt;
            var oldPos = p.Pos;
            p.Pos += p.Vel * dt;
            p.Vel *= MathF.Max(0f, 1f - p.Drag * dt);

            if (p.Kind == Kind.Puff && p.Vel.LengthSquared() > 0.01f)
            {
                var tx = (int) MathF.Floor(p.Pos.X);
                var ty = (int) MathF.Floor(p.Pos.Y);
                if (tx != p.TileX || ty != p.TileY)
                {
                    p.TileX = tx;
                    p.TileY = ty;

                    if (_world.Blocked(p.Map, p.Pos))
                    {
                        p.Pos = oldPos;
                        p.Vel = Vector2.Zero;
                        p.Life = MathF.Min(p.Life, p.Age + 0.4f);
                    }
                }
            }

            if (p.Rise != 0f)
                p.Z += p.Rise * dt;

            if (!p.Falls)
                continue;

            p.VZ -= 9f * dt;
            p.Z += p.VZ * dt;

            if (p.Z > 0f)
                continue;

            if (p.Bounces > 0)
            {
                p.Bounces--;
                p.Z = 0f;
                p.VZ = -p.VZ * 0.5f;
                p.Vel *= 0.6f;
            }
            else
            {
                _particles.RemoveAt(i);
            }
        }
    }

    private void UpdateRibbons(float dt)
    {
        _trailPoints = 0;

        for (var i = _ribbons.Count - 1; i >= 0; i--)
        {
            var r = _ribbons[i];
            var pts = r.Points;

            var expired = 0;
            foreach (var p in pts)
            {
                p.Age += dt;
                if (p.Age > r.Life)
                    expired++;
            }

            if (expired > 0)
                pts.RemoveRange(0, Math.Min(expired, pts.Count));

            foreach (var p in pts)
            {
                var target = _world.Visible(r.Map, p.Pos) ? 1f : 0f;
                p.Vis = p.Vis < 0f ? target : p.Vis + Math.Clamp(target - p.Vis, -dt * 7f, dt * 7f);
            }

            if (!r.Open && pts.Count == 0)
            {
                _ribbons.RemoveAt(i);
                continue;
            }

            _trailPoints += pts.Count;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var view = args.WorldAABB.Enlarged(5f);

        DrawRibbons(handle, view, args.MapId);

        foreach (var st in _states.Values)
        {
            if (!st.Visible || st.Map != args.MapId || !view.Contains(st.Pos) || !_world.Visible(st.Map, st.Pos))
                continue;

            DrawTracer(handle, st);
        }

        foreach (var p in _particles)
        {
            if (p.Kind == Kind.Puff)
                DrawParticle(handle, p, view, args.MapId);
        }

        foreach (var p in _particles)
        {
            if (p.Kind != Kind.Puff)
                DrawParticle(handle, p, view, args.MapId);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawRibbons(DrawingHandleWorld handle, Box2 view, MapId map)
    {
        foreach (var r in _ribbons)
        {
            if (r.Map != map || r.Points.Count < 2)
                continue;

            var pts = r.Points;
            var prev = Displace(r, pts[0]);

            for (var i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1];
                var b = pts[i];
                var pb = Displace(r, b);
                var pa = prev;
                prev = pb;

                var vis = MathF.Min(a.Vis, b.Vis);
                if (vis <= 0.01f || !view.Contains(pb))
                    continue;

                var d = pb - pa;
                var len = d.Length();
                if (len < 0.01f)
                    continue;

                var t = Math.Clamp((a.Age + b.Age) * 0.5f / r.Life, 0f, 1f);

                var fromHead = MathF.Max(0f, r.HeadDist - (a.Dist + b.Dist) * 0.5f);
                var alpha = r.Alpha * Smooth(0f, 0.9f, fromHead) * MathF.Pow(1f - t, 1.1f) * vis;
                alpha *= 0.9f + 0.1f * b.Seed;

                var taper = 0.30f + 0.70f * Smooth(0f, 2.2f, fromHead);
                var width = r.Width * (0.5f + 1.9f * MathF.Pow(t, 0.6f)) * taper;

                var heat = MathF.Exp(-(a.Age + b.Age) * 0.5f / 0.18f);
                var color = Color.InterpolateBetween(r.Color, r.Warm, heat * 0.55f);

                var tex = _wisps[a.Variant];
                var center = (pa + pb) * 0.5f;
                var angle = MathF.Atan2(d.Y, d.X);

                DrawTex(handle, tex, center, angle, new Vector2(len * 2.0f, width * 2.1f), Tint(color, alpha * 0.34f));
                DrawTex(handle, tex, center, angle, new Vector2(len * 2.0f, width), Tint(color, alpha));
            }
        }
    }

    private static float Smooth(float a, float b, float x)
    {
        var t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private void DrawParticle(DrawingHandleWorld handle, Particle p, Box2 view, MapId map)
    {
        if (p.Map != map || p.Vis <= 0.01f || !view.Contains(p.Pos))
            return;

        var t = Math.Clamp(p.Age / p.Life, 0f, 1f);
        var drawPos = p.Pos + new Vector2(0f, p.Z * 0.55f);

        switch (p.Kind)
        {
            case Kind.Puff:
                DrawAnim(handle, p.Frames!, t, drawPos, p.Rot, new Vector2(p.Size), SmokeColor(p, t), p.Alpha * p.Vis, true);
                break;

            case Kind.Impact:
                DrawAnim(handle, p.Frames!, t, drawPos, p.Rot, new Vector2(p.Size), Color.White, p.Vis, false);
                break;

            case Kind.Spark:
            {
                var rot = MathF.Atan2(p.Vel.Y + p.VZ * 0.55f, p.Vel.X);
                var speed = MathF.Sqrt(p.Vel.LengthSquared() + p.VZ * p.VZ * 0.3f);
                var len = MathF.Max(p.Size * 0.4f, p.Size * Math.Clamp(speed / 12f, 0.3f, 1f));
                handle.SetTransform(Matrix3Helpers.CreateTransform(drawPos, rot));
                handle.DrawRect(new Box2(-len, -Px * 0.75f, 0f, Px * 0.75f), Tint(p.Color, (1f - t) * p.Vis));
                break;
            }
        }
    }

    private static Color SmokeColor(Particle p, float t)
        => p.Lit is { } lit
            ? Color.InterpolateBetween(lit, p.Color, Math.Clamp(t / p.LitFrac, 0f, 1f))
            : p.Color;

    private static void DrawAnim(DrawingHandleWorld handle, Texture[] frames, float t, Vector2 center, float rot,
        Vector2 size, Color color, float alpha, bool blend)
    {
        var pos = Math.Clamp(t, 0f, 1f) * (frames.Length - 1);
        var i = (int) pos;
        var f = pos - i;

        if (!blend || i >= frames.Length - 1)
        {
            DrawTex(handle, frames[Math.Min(i, frames.Length - 1)], center, rot, size, Tint(color, alpha));
            return;
        }

        DrawTex(handle, frames[i], center, rot, size, Tint(color, alpha * (1f - f)));
        DrawTex(handle, frames[i + 1], center, rot, size, Tint(color, alpha * f));
    }

    private static void DrawTex(DrawingHandleWorld handle, Texture tex, Vector2 center, float rot, Vector2 size, Color color)
    {
        if (color.A <= 0.01f)
            return;

        handle.SetTransform(Matrix3Helpers.CreateTransform(center, rot));
        handle.DrawTextureRect(tex, Box2.CenteredAround(Vector2.Zero, size), color);
    }

    private void DrawTracer(DrawingHandleWorld handle, ShotState st)
    {
        var c = st.Comp;
        var angle = MathF.Atan2(st.Dir.Y, st.Dir.X);
        var len = MathF.Min(c.Length, st.Dist + 0.2f);
        var core = MathF.Max(Px * 1.5f, c.Width);
        var halo = core * 3f;
        var flick = 0.92f + 0.08f * MathF.Sin(st.Age * 60f + st.Phase);
        var seg = len / TracerSteps;

        handle.SetTransform(Matrix3Helpers.CreateTransform(st.Pos, angle));

        for (var i = 0; i < TracerSteps; i++)
        {
            var f = i / (float) (TracerSteps - 1);
            var fade = MathF.Pow(1f - f, 1.5f) + 0.05f;
            var taper = 1f - 0.55f * f;
            var x1 = -seg * i;
            var x0 = -seg * (i + 1);

            handle.DrawRect(new Box2(x0, -halo * taper * 0.5f, x1, halo * taper * 0.5f),
                c.TracerColor.WithAlpha(0.20f * fade * flick));

            var col = Color.InterpolateBetween(c.CoreColor, c.TracerColor, f * 0.85f);
            handle.DrawRect(new Box2(x0, -core * taper * 0.5f, x1, core * taper * 0.5f),
                col.WithAlpha(MathF.Min(1f, fade * 1.1f)));
        }

        handle.DrawRect(new Box2(-Px * 2f, -core * 0.75f, Px * 1.5f, core * 0.75f), c.CoreColor);
    }

    private static Color Tint(Color c, float alpha)
        => new(c.R, c.G, c.B, Math.Clamp(alpha, 0f, 1f));
}
