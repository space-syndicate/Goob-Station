using System.Numerics;
using Content.Shared._BlackM.Rappel;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._BlackM.Rappel;

public sealed class RappelDebrisVisualsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float BounceHeight = 0.08f;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var now = _timing.CurTime;
        var floorDepth = (int) DrawDepth.FloorObjects;
        var effectsDepth = (int) DrawDepth.Effects;

        var debrisQuery = EntityQueryEnumerator<RappelFallingDebrisComponent, SpriteComponent>();
        while (debrisQuery.MoveNext(out var uid, out var debris, out var sprite))
        {
            var ent = (uid, sprite);
            var t = (float) (now - debris.StartTime).TotalSeconds;
            if (t < 0f)
            {
                if (sprite.Visible)
                    _sprite.SetVisible(ent, false);
                continue;
            }

            var scale = MathF.Max(0.05f, debris.Scale * debris.ScaleMultiplier);
            var scaleVec = new Vector2(scale, scale);
            var fallTime = MathF.Max(0.05f, debris.FallTime);
            var landed = t - fallTime;

            if (landed >= 0f)
            {
                var fadeTime = MathF.Max(0.05f, debris.FadeTime);
                var fade = Math.Clamp((landed - MathF.Max(0f, debris.FadeAfter)) / fadeTime, 0f, 1f);
                var alpha = 1f - fade;
                var visible = alpha > 0f;
                if (sprite.Visible != visible)
                    _sprite.SetVisible(ent, visible);
                if (!visible)
                    continue;

                var bounce = MathF.Sin(MathF.Min(1f, landed / fadeTime) * MathF.PI) * BounceHeight / MathF.Max(1f, scale);
                _sprite.SetDrawDepth(ent, floorDepth);
                _sprite.SetOffset(ent, new Vector2(0f, bounce));
                _sprite.SetRotation(ent, debris.FinalRotation);
                _sprite.SetScale(ent, scaleVec * (1f - 0.25f * fade));
                _sprite.SetColor(ent, sprite.Color.WithAlpha(alpha));
                continue;
            }

            if (!sprite.Visible)
                _sprite.SetVisible(ent, true);
            _sprite.SetScale(ent, scaleVec);
            _sprite.SetColor(ent, sprite.Color.WithAlpha(1f));

            var u = t / fallTime;
            var y = debris.FallHeight * (1f - u * u);
            var x = -debris.Drift * (1f - u);
            _sprite.SetDrawDepth(ent, effectsDepth);
            _sprite.SetOffset(ent, new Vector2(x, y));
            _sprite.SetRotation(ent, debris.FinalRotation + Angle.FromDegrees(debris.Spin * (1f - u)));
        }

        var dustQuery = EntityQueryEnumerator<RappelDustComponent, SpriteComponent>();
        while (dustQuery.MoveNext(out var uid, out var dust, out var sprite))
        {
            var ent = (uid, sprite);
            var t = (float) (now - dust.StartTime).TotalSeconds;
            var duration = MathF.Max(0.1f, dust.Duration);
            if (t < 0f || t >= duration)
            {
                if (sprite.Visible)
                    _sprite.SetVisible(ent, false);
                continue;
            }

            if (!sprite.Visible)
                _sprite.SetVisible(ent, true);

            var u = t / duration;
            var ease = 1f - (1f - u) * (1f - u);
            var scale = dust.StartScale + (dust.EndScale - dust.StartScale) * ease;
            var alpha = dust.MaxAlpha * MathF.Min(1f, u * 6f) * MathF.Pow(1f - u, 1.5f);

            _sprite.SetScale(ent, new Vector2(scale, scale));
            _sprite.SetOffset(ent, dust.Drift * ease + new Vector2(0f, dust.Height - dust.Drop * ease));
            _sprite.SetColor(ent, sprite.Color.WithAlpha(alpha));
        }
    }
}
