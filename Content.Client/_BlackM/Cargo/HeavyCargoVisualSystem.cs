using System.Numerics;
using Content.Shared._BlackM.Cargo;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._BlackM.Cargo;

public sealed class HeavyCargoVisualSystem : EntitySystem
{
    private const int BaseLayer = 0;
    private const int BlockedLayer = 1;

    private const float BottomScale = HeavyCargoTiming.BottomScale;
    private const float BottomBrightness = 0.3f;
    private const float MoveTime = 2.9f;
    private const float VanishTime = 0.9f;
    private const float AppearTime = 0.3f;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly record struct Original(Vector2 Scale, Color Color, bool Visible);
    private readonly record struct ActiveLift(EntityUid Parent, Vector2 Center, Angle Rotation, float Half,
        CargoMachineVisualComponent Comp);

    private readonly Dictionary<EntityUid, Original> _original = new();
    private readonly List<ActiveLift> _active = new();
    private readonly HashSet<EntityUid> _seen = new();
    private readonly List<EntityUid> _restore = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoMachineVisualComponent, AppearanceChangeEvent>(OnAppearance);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        foreach (var (uid, original) in _original)
            Restore(uid, original);
        _original.Clear();
    }

    private void OnAppearance(EntityUid uid, CargoMachineVisualComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        _appearance.TryGetData<CargoPhase>(uid, CargoVisuals.Phase, out var phase, args.Component);
        _appearance.TryGetData<bool>(uid, CargoVisuals.Paused, out var paused, args.Component);

        var ent = (uid, sprite);

        if (!_appearance.TryGetData<bool>(uid, CargoVisuals.Export, out var export, args.Component))
            export = HasState(ent, HeavyCargoTiming.StateName(CargoPhase.Idle, true));

        var state = HeavyCargoTiming.StateName(phase, export);
        if (!HasState(ent, state))
        {
            var other = HeavyCargoTiming.StateName(phase, !export);
            if (!HasState(ent, other))
            {
                Log.Warning($"{ToPrettyString(uid)}: cargo lift RSI has neither '{state}' nor '{other}'.");
                return;
            }
            state = other;
            export = !export;
        }

        if (comp.AppliedState != state)
        {
            comp.AppliedState = state;
            comp.PhaseTime = 0f;
            _sprite.LayerSetRsiState(ent, BaseLayer, state);
            _sprite.LayerSetAnimationTime(ent, BaseLayer, 0f);
        }
        comp.Phase = phase;
        comp.Export = export;
        comp.Paused = paused;

        _sprite.LayerSetAutoAnimated(ent, BaseLayer, !paused || phase == CargoPhase.Idle);
        _sprite.LayerSetVisible(ent, BlockedLayer, paused);
    }

    private bool HasState(Entity<SpriteComponent> ent, string state)
    {
        if (!_sprite.TryGetLayer((ent.Owner, ent.Comp), BaseLayer, out var layer, false))
            return false;
        var rsi = layer.ActualRsi ?? ent.Comp.BaseRSI;
        return rsi != null && rsi.TryGetState(state, out _);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _active.Clear();
        var machines = EntityQueryEnumerator<CargoMachineVisualComponent, TransformComponent>();
        while (machines.MoveNext(out _, out var comp, out var xform))
        {
            if (!comp.Paused)
                comp.PhaseTime += frameTime;
            if (comp.Phase == CargoPhase.Idle)
                continue;
            var center = xform.LocalPosition + xform.LocalRotation.RotateVec(comp.PadOffset);
            _active.Add(new ActiveLift(xform.ParentUid, center, xform.LocalRotation, comp.PadHalf + 0.25f, comp));
        }

        _seen.Clear();
        var items = EntityQueryEnumerator<CargoTransitComponent, SpriteComponent, TransformComponent>();
        while (items.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            CargoMachineVisualComponent? lift = null;
            foreach (var active in _active)
            {
                if (active.Parent != xform.ParentUid)
                    continue;
                var local = (-active.Rotation).RotateVec(xform.LocalPosition - active.Center);
                if (MathF.Abs(local.X) <= active.Half && MathF.Abs(local.Y) <= active.Half)
                {
                    lift = active.Comp;
                    break;
                }
            }

            if (!_original.TryGetValue(uid, out var original))
            {
                if (lift == null)
                    continue;
                original = new Original(sprite.Scale, sprite.Color, sprite.Visible);
                _original[uid] = original;
            }
            _seen.Add(uid);

            var (depth, alpha) = lift == null ? (0f, 1f) : ItemDepth(lift);
            Apply((uid, sprite), original, depth, alpha);
        }

        _restore.Clear();
        foreach (var uid in _original.Keys)
        {
            if (!_seen.Contains(uid))
                _restore.Add(uid);
        }
        foreach (var uid in _restore)
        {
            Restore(uid, _original[uid]);
            _original.Remove(uid);
        }
    }

    private static (float Depth, float Alpha) ItemDepth(CargoMachineVisualComponent lift)
    {
        var t = lift.PhaseTime;
        if (lift.Export)
        {
            return lift.Phase switch
            {
                CargoPhase.Out => (Ease(t / MoveTime), 1f),
                CargoPhase.Jaws => (1f, 1f - Math.Clamp(t / VanishTime, 0f, 1f)),
                CargoPhase.Return or CargoPhase.Closing => (1f, 0f),
                _ => (0f, 1f),
            };
        }
        return lift.Phase switch
        {
            CargoPhase.Warning or CargoPhase.Opening => (1f, 0f),
            CargoPhase.Out => (1f - Ease(t / MoveTime), Math.Clamp(t / AppearTime, 0f, 1f)),
            _ => (0f, 1f),
        };
    }

    private void Apply(Entity<SpriteComponent> ent, Original original, float depth, float alpha)
    {
        var scale = 1f + (BottomScale - 1f) * depth;
        var light = 1f + (BottomBrightness - 1f) * depth;
        var color = original.Color;
        _sprite.SetScale((ent.Owner, ent.Comp), original.Scale * scale);
        _sprite.SetColor((ent.Owner, ent.Comp),
            new Color(color.R * light, color.G * light, color.B * light, color.A * alpha));
        _sprite.SetVisible((ent.Owner, ent.Comp), original.Visible && alpha > 0.01f);
    }

    private void Restore(EntityUid uid, Original original)
    {
        if (TerminatingOrDeleted(uid) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;
        _sprite.SetScale((uid, sprite), original.Scale);
        _sprite.SetColor((uid, sprite), original.Color);
        _sprite.SetVisible((uid, sprite), original.Visible);
    }

    private static float Ease(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
