using System.Numerics;
using Content.Client.Animations;
using Content.Shared._CorvaxGoob.Weapons.Melee.SwipeCombat;
using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._CorvaxGoob.Weapons.Melee.SwipeCombat;

/// <summary>
/// Анимации свайп-ударов:
/// сверху вниз — монтировка плывёт от цели к атакующему (притягивание);
/// снизу вверх — от атакующего к цели (тычок);
/// влево/вправо — дуга относительно персонажа.
/// Особый удар рисуется монтировкой с синим контуром.
/// </summary>
public sealed class SwipeCombatVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string MoveKey = "swipe-combat-move";
    private const string FadeKey = "swipe-combat-fade";

    /// <summary>Ближняя к атакующему точка траектории.</summary>
    private const float NearDistance = 0.35f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SwipeAttackVisualEvent>(OnVisual);
    }

    private void OnVisual(SwipeAttackVisualEvent ev)
    {
        var user = GetEntity(ev.User);
        var weapon = GetEntity(ev.Weapon);

        if (!Exists(user) || ev.LocalDirection.LengthSquared() < 0.0001f)
            return;

        if (!TryComp<SwipeCombatComponent>(weapon, out var swipe) || !TryComp<MeleeWeaponComponent>(weapon, out var melee))
            return;

        var userXform = Transform(user);
        if (userXform.MapID == MapId.Nullspace)
            return;

        var fx = Spawn(swipe.Effect, userXform.Coordinates);
        if (!TryComp<SpriteComponent>(fx, out var sprite))
            return;

        // Обычный удар — спрайт самого оружия, особый — монтировка с синим контуром из прототипа эффекта.
        if (!ev.Special && TryComp<SpriteComponent>(weapon, out var weaponSprite))
            _sprite.CopySprite((weapon, weaponSprite), (fx, sprite));

        var track = EnsureComp<TrackUserComponent>(fx);
        track.User = user;
        sprite.NoRotation = true;

        var length = Math.Clamp(0.6f / melee.AttackRate, 0.3f, 0.8f);
        var reach = MathF.Max(NearDistance + 0.3f, ev.Range - 0.2f);
        var aim = ev.LocalDirection.ToWorldAngle();
        var spriteRotation = melee.WideAnimationRotation;

        var move = ev.Direction switch
        {
            SwipeAttackDirection.Overhead => GetLine(aim, spriteRotation, reach, NearDistance, length),
            SwipeAttackDirection.Thrust => GetLine(aim, spriteRotation, NearDistance, reach, length),
            _ => GetArc(aim, ev.Arc, ev.Direction == SwipeAttackDirection.SlashRight, spriteRotation, reach * 0.85f, length),
        };

        _animation.Play(fx, move, MoveKey);
        _animation.Play(fx, GetFade(sprite, length * 0.55f, length), FadeKey);
    }

    /// <summary>
    /// Монтировка летит по прямой вдоль направления удара: от <paramref name="from"/> до <paramref name="to"/> (в тайлах от атакующего).
    /// </summary>
    private Robust.Client.Animations.Animation GetLine(Angle aim, Angle spriteRotation, float from, float to, float length)
    {
        var start = aim.RotateVec(new Vector2(0f, -from));
        var end = aim.RotateVec(new Vector2(0f, -to));
        var rotation = aim + spriteRotation;

        return new Robust.Client.Animations.Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(rotation, 0f),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        // Время ключевого кадра — от предыдущего кадра.
                        new AnimationTrackProperty.KeyFrame(start, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Lerp(start, end, 0.7f), length * 0.15f),
                        new AnimationTrackProperty.KeyFrame(end, length * 0.2f),
                        new AnimationTrackProperty.KeyFrame(end, length * 0.65f),
                    },
                },
            },
        };
    }

    /// <summary>
    /// Монтировка проходит дугой вокруг атакующего.
    /// <paramref name="toCharacterRight"/> — удар слева направо относительно персонажа.
    /// </summary>
    private Robust.Client.Animations.Animation GetArc(Angle aim, Angle arc, bool toCharacterRight,
        Angle spriteRotation, float radius, float length)
    {
        // Угол растёт против часовой стрелки: aim + arc/2 — левый край дуги относительно персонажа.
        var left = aim + arc * 0.5f;
        var right = aim - arc * 0.5f;
        var (start, end) = toCharacterRight ? (left, right) : (right, left);

        var rotation = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Rotation),
        };
        var offset = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Offset),
        };

        // Несколько точек, чтобы монтировка шла по окружности, а не по хорде.
        const int steps = 6;
        var sweepTime = length * 0.4f;
        for (var i = 0; i <= steps; i++)
        {
            var t = (float) i / steps;
            var angle = Angle.Lerp(start, end, t);
            var time = i == 0 ? 0f : sweepTime / steps;
            rotation.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(angle + spriteRotation, time));
            offset.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(angle.RotateVec(new Vector2(0f, -radius)), time));
        }

        return new Robust.Client.Animations.Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks = { rotation, offset },
        };
    }

    private static Robust.Client.Animations.Animation GetFade(SpriteComponent sprite, float start, float end)
    {
        return new Robust.Client.Animations.Animation
        {
            Length = TimeSpan.FromSeconds(end),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, start),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), end - start),
                    },
                },
            },
        };
    }
}
